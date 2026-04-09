using ComplaintClassifier.Domain.Messages;

namespace ComplaintClassifier.Application.Contracts;

public interface IQueuePublisher
{
    Task PublishClassificationRequestedAsync(QueueMessage message, CancellationToken cancellationToken);
    Task PublishProcessingRequestedAsync(QueueMessage message, CancellationToken cancellationToken);
}
