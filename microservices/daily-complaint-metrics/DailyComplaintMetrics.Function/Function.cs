using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Domain.Messages;
using DailyComplaintMetrics.Function.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DailyComplaintMetrics.Function;

public sealed class Function
{
    private const int DefaultQueryLimit = 100;
    private const int MaxQueryLimit = 500;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly UpdateDailyMetricsHandler _updateHandler;
    private readonly GetDailyMetricsHandler _getHandler;
    private readonly GetMetricMessageEventsHandler _getMetricMessageEventsHandler;
    private readonly GetProcessedMessagesHandler _getProcessedMessagesHandler;
    private readonly ILogger<Function> _logger;

    public Function()
        : this(
            ServiceProviderFactory.GetProvider().GetRequiredService<UpdateDailyMetricsHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<GetDailyMetricsHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<GetMetricMessageEventsHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<GetProcessedMessagesHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<ILogger<Function>>())
    {
    }

    public Function(
        UpdateDailyMetricsHandler updateHandler,
        GetDailyMetricsHandler getHandler,
        GetMetricMessageEventsHandler getMetricMessageEventsHandler,
        GetProcessedMessagesHandler getProcessedMessagesHandler,
        ILogger<Function> logger)
    {
        _updateHandler = updateHandler;
        _getHandler = getHandler;
        _getMetricMessageEventsHandler = getMetricMessageEventsHandler;
        _getProcessedMessagesHandler = getProcessedMessagesHandler;
        _logger = logger;
    }

    public async Task<object> FunctionHandler(JsonElement input, ILambdaContext context)
    {
        if (IsSqsEvent(input))
        {
            return await HandleSqsAsync(input);
        }

        if (IsApiGatewayEvent(input))
        {
            return await HandleApiAsync(input);
        }

        _logger.LogWarning("Unsupported event payload for DailyComplaintMetrics lambda");
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Body = JsonSerializer.Serialize(new { error = "Unsupported event payload" }, JsonSerializerOptions),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };
    }

    private async Task<SQSBatchResponse> HandleSqsAsync(JsonElement input)
    {
        var sqsEvent = JsonSerializer.Deserialize<SQSEvent>(input.GetRawText(), JsonSerializerOptions)
            ?? throw new InvalidOperationException("Evento SQS invalido.");

        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in sqsEvent.Records)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<MetricsEventMessage>(record.Body, JsonSerializerOptions)
                    ?? throw new InvalidOperationException("Mensagem de metrica invalida.");

                await _updateHandler.HandleAsync(payload, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Daily metrics update failed. messageId={MessageId}", record.MessageId);
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
            }
        }

        return new SQSBatchResponse(failures);
    }

    private async Task<APIGatewayProxyResponse> HandleApiAsync(JsonElement input)
    {
        var method = GetHttpMethod(input);
        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return BuildResponse(HttpStatusCode.MethodNotAllowed, new { error = "Metodo nao suportado." });
        }

        var path = NormalizePath(GetRawPath(input));

        try
        {
            if (string.Equals(path, "/metrics", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleGetMetricsAsync(input);
            }

            if (string.Equals(path, "/metrics/events", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleGetMetricEventsAsync(input);
            }

            if (string.Equals(path, "/metrics/processed", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleGetProcessedMessagesAsync(input);
            }

            return BuildResponse(HttpStatusCode.NotFound, new { error = $"Rota nao encontrada: {path}" });
        }
        catch (ArgumentException exception)
        {
            return BuildResponse(HttpStatusCode.BadRequest, new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BuildResponse(HttpStatusCode.BadRequest, new { error = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Daily metrics GET request failed. path={Path}", path);
            return BuildResponse(HttpStatusCode.InternalServerError, new { error = "Falha interna ao consultar metricas." });
        }
    }

    private async Task<APIGatewayProxyResponse> HandleGetMetricsAsync(JsonElement input)
    {
        var day = GetQueryStringParameter(input, "day");
        if (string.IsNullOrWhiteSpace(day) || !Regex.IsMatch(day, "^\\d{8}$"))
        {
            throw new ArgumentException("Parametro 'day' obrigatorio no formato yyyyMMdd.");
        }

        var metrics = await _getHandler.HandleAsync(day, CancellationToken.None);

        return BuildResponse(HttpStatusCode.OK, new
        {
            day = metrics.Day,
            receivedCount = metrics.ReceivedCount,
            classifiedCount = metrics.ClassifiedCount,
            classificationFailedCount = metrics.ClassificationFailedCount,
            processedSuccessCount = metrics.ProcessedSuccessCount,
            processedErrorCount = metrics.ProcessedErrorCount,
            createdAtUtc = metrics.CreatedAtUtc,
            updatedAtUtc = metrics.UpdatedAtUtc
        });
    }

    private async Task<APIGatewayProxyResponse> HandleGetMetricEventsAsync(JsonElement input)
    {
        var day = GetQueryStringParameter(input, "day");
        if (string.IsNullOrWhiteSpace(day) || !Regex.IsMatch(day, "^\\d{8}$"))
        {
            throw new ArgumentException("Parametro 'day' obrigatorio no formato yyyyMMdd.");
        }

        var eventType = GetQueryStringParameter(input, "eventType");
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Parametro 'eventType' obrigatorio.");
        }

        var limit = ReadLimit(input);
        var items = await _getMetricMessageEventsHandler.HandleAsync(day, eventType, limit, CancellationToken.None);

        return BuildResponse(HttpStatusCode.OK, new
        {
            day,
            eventType = eventType.Trim().ToUpperInvariant(),
            total = items.Count,
            items = items.Select(item => new
            {
                complaintId = item.ComplaintId,
                correlationId = item.CorrelationId,
                eventType = item.EventType,
                eventCreatedAtUtc = item.EventCreatedAtUtc
            })
        });
    }

    private async Task<APIGatewayProxyResponse> HandleGetProcessedMessagesAsync(JsonElement input)
    {
        var complaintId = GetQueryStringParameter(input, "complaintId");
        var correlationId = GetQueryStringParameter(input, "correlationId");
        var hasComplaintId = !string.IsNullOrWhiteSpace(complaintId);
        var hasCorrelationId = !string.IsNullOrWhiteSpace(correlationId);

        if (hasComplaintId == hasCorrelationId)
        {
            throw new ArgumentException("Informe apenas um filtro: 'complaintId' ou 'correlationId'.");
        }

        var limit = ReadLimit(input);
        var items = hasComplaintId
            ? await _getProcessedMessagesHandler.HandleByComplaintIdAsync(complaintId!, limit, CancellationToken.None)
            : await _getProcessedMessagesHandler.HandleByCorrelationIdAsync(correlationId!, limit, CancellationToken.None);

        var searchBy = hasComplaintId ? "complaintId" : "correlationId";
        var searchValue = hasComplaintId ? complaintId! : correlationId!;

        return BuildResponse(HttpStatusCode.OK, new
        {
            searchBy,
            searchValue,
            total = items.Count,
            items = items.Select(item => new
            {
                complaintId = item.ComplaintId,
                correlationId = item.CorrelationId,
                day = item.Day,
                processedAtUtc = item.EventCreatedAtUtc
            })
        });
    }

    private static string? GetHttpMethod(JsonElement input)
    {
        if (input.TryGetProperty("httpMethod", out var httpMethod))
        {
            return httpMethod.GetString();
        }

        if (input.TryGetProperty("requestContext", out var requestContext)
            && requestContext.TryGetProperty("http", out var http)
            && http.TryGetProperty("method", out var method))
        {
            return method.GetString();
        }

        return null;
    }

    private static string? GetQueryStringParameter(JsonElement input, string key)
    {
        if (!input.TryGetProperty("queryStringParameters", out var queryParams)
            || queryParams.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (queryParams.TryGetProperty(key, out var value))
        {
            return value.GetString();
        }

        foreach (var property in queryParams.EnumerateObject())
        {
            if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private static string GetRawPath(JsonElement input)
    {
        if (input.TryGetProperty("rawPath", out var rawPath) && !string.IsNullOrWhiteSpace(rawPath.GetString()))
        {
            return rawPath.GetString()!;
        }

        if (input.TryGetProperty("path", out var path) && !string.IsNullOrWhiteSpace(path.GetString()))
        {
            return path.GetString()!;
        }

        return "/metrics";
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }

    private static int ReadLimit(JsonElement input)
    {
        var rawLimit = GetQueryStringParameter(input, "limit");
        if (string.IsNullOrWhiteSpace(rawLimit))
        {
            return DefaultQueryLimit;
        }

        if (!int.TryParse(rawLimit, out var parsed))
        {
            throw new ArgumentException("Parametro 'limit' invalido. Use um numero inteiro.");
        }

        return Math.Clamp(parsed, 1, MaxQueryLimit);
    }

    private static APIGatewayProxyResponse BuildResponse(HttpStatusCode statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(body, JsonSerializerOptions),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        };
    }

    private static bool IsSqsEvent(JsonElement input)
    {
        if (!input.TryGetProperty("Records", out var records) || records.ValueKind != JsonValueKind.Array || records.GetArrayLength() == 0)
        {
            return false;
        }

        var first = records[0];
        return first.TryGetProperty("eventSource", out var source)
               && string.Equals(source.GetString(), "aws:sqs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsApiGatewayEvent(JsonElement input)
    {
        return input.TryGetProperty("httpMethod", out _)
               || (input.TryGetProperty("requestContext", out var context) && context.TryGetProperty("http", out _));
    }
}
