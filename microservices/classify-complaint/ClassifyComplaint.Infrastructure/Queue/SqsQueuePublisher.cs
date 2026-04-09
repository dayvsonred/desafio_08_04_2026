using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Messages;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Infrastructure.Queue;

public sealed class SqsQueuePublisher : IQueuePublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonSQS _sqs;
    private readonly AwsResourceOptions _options;

    public SqsQueuePublisher(IAmazonSQS sqs, IOptions<AwsResourceOptions> options)
    {
        _sqs = sqs;
        _options = options.Value;
    }

    public Task PublishClassificationRequestedAsync(QueueMessage message, CancellationToken cancellationToken)
        => PublishAsync(_options.ClassificationQueueUrl, message, cancellationToken);

    public Task PublishProcessingRequestedAsync(QueueMessage message, CancellationToken cancellationToken)
        => PublishAsync(_options.ProcessingQueueUrl, message, cancellationToken);

    private async Task PublishAsync(string queueUrl, QueueMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            throw new InvalidOperationException("Queue URL não configurada.");
        }

        var body = JsonSerializer.Serialize(message, JsonSerializerOptions);

        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = body
        }, cancellationToken);
    }
}
