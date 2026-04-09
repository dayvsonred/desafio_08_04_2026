using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Domain.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcessClassifiedComplaint.Function.Bootstrap;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ProcessClassifiedComplaint.Function;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ProcessClassifiedComplaintHandler _handler;
    private readonly ILogger<Function> _logger;

    public Function()
        : this(
            ServiceProviderFactory.GetProvider().GetRequiredService<ProcessClassifiedComplaintHandler>(),
            ServiceProviderFactory.GetProvider().GetRequiredService<ILogger<Function>>())
    {
    }

    public Function(ProcessClassifiedComplaintHandler handler, ILogger<Function> logger)
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
                    ?? throw new InvalidOperationException("Mensagem SQS vazia para processamento.");

                await _handler.HandleAsync(payload.ComplaintId, payload.CorrelationId, record.MessageId, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "ProcessClassifiedComplaint failed for messageId={MessageId}", record.MessageId);
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
            }
        }

        return new SQSBatchResponse(failures);
    }
}