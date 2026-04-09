using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Domain.Messages;
using ClassifyComplaint.Function.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ClassifyComplaint.Function;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ClassifyComplaintHandler _handler;
    private readonly ILogger<Function> _logger;

    public Function()
        : this(
            ServiceProviderFactory.GetProvider().GetRequiredService<ClassifyComplaintHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<ILogger<Function>>())
    {
    }

    public Function(ClassifyComplaintHandler handler, ILogger<Function> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in sqsEvent.Records)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<QueueMessage>(record.Body, JsonSerializerOptions)
                    ?? throw new InvalidOperationException("Mensagem SQS vazia para classificacao.");

                await _handler.HandleAsync(payload.ComplaintId, payload.CorrelationId, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "ClassifyComplaint failed for messageId={MessageId}", record.MessageId);
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
            }
        }

        return new SQSBatchResponse(failures);
    }
}
