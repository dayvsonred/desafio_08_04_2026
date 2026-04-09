using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ComplaintClassifier.Application.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReceiveComplaint.Function.Bootstrap;
using ReceiveComplaint.Function.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ReceiveComplaint.Function;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ReceiveComplaintHandler _handler;
    private readonly ILogger<Function> _logger;

    public Function()
        : this(
            ServiceProviderFactory.GetProvider().GetRequiredService<ReceiveComplaintHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<ILogger<Function>>())
    {
    }

    public Function(ReceiveComplaintHandler handler, ILogger<Function> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var correlationId = GetHeaderValue(request.Headers, "x-correlation-id");

        ReceiveComplaintApiRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ReceiveComplaintApiRequest>(request.Body ?? string.Empty, JsonSerializerOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Invalid payload JSON. correlationId={CorrelationId}", correlationId);
            return BuildResponse(HttpStatusCode.BadRequest, new { error = "Payload JSON invalido." });
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Reclamacao))
        {
            return BuildResponse(HttpStatusCode.BadRequest, new { error = "Campo 'reclamacao' e obrigatorio." });
        }

        try
        {
            var result = await _handler.HandleAsync(payload.Reclamacao, correlationId, CancellationToken.None);

            return BuildResponse(HttpStatusCode.Accepted, new
            {
                complaintId = result.ComplaintId,
                correlationId = result.CorrelationId,
                status = result.Status.ToString()
            });
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Validation error. correlationId={CorrelationId}", correlationId);
            return BuildResponse(HttpStatusCode.BadRequest, new { error = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "ReceiveComplaint failed. correlationId={CorrelationId}", correlationId);
            return BuildResponse(HttpStatusCode.InternalServerError, new { error = "Erro interno ao registrar reclamacao." });
        }
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

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string key)
    {
        if (headers is null)
        {
            return null;
        }

        if (headers.TryGetValue(key, out var value))
        {
            return value;
        }

        var match = headers.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(match.Key) ? null : match.Value;
    }
}
