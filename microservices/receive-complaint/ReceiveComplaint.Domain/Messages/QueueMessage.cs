namespace ComplaintClassifier.Domain.Messages;

public sealed class QueueMessage
{
    public required string ComplaintId { get; init; }
    public required string CorrelationId { get; init; }
    public required string EventType { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}
