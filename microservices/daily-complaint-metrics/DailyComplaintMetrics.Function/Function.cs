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
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly UpdateDailyMetricsHandler _updateHandler;
    private readonly GetDailyMetricsHandler _getHandler;
    private readonly ILogger<Function> _logger;

    public Function()
        : this(
            ServiceProviderFactory.GetProvider().GetRequiredService<UpdateDailyMetricsHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<GetDailyMetricsHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<ILogger<Function>>())
    {
    }

    public Function(UpdateDailyMetricsHandler updateHandler, GetDailyMetricsHandler getHandler, ILogger<Function> logger)
    {
        _updateHandler = updateHandler;
        _getHandler = getHandler;
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

        var day = GetQueryStringParameter(input, "day");
        if (string.IsNullOrWhiteSpace(day) || !Regex.IsMatch(day, "^\\d{8}$"))
        {
            return BuildResponse(HttpStatusCode.BadRequest, new { error = "Parametro 'day' obrigatorio no formato yyyyMMdd." });
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
