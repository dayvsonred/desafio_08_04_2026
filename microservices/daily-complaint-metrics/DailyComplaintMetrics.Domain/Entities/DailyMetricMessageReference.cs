namespace ComplaintClassifier.Domain.Entities;

public sealed class DailyMetricMessageReference
{
    public required string Day { get; init; }
    public required string ComplaintId { get; init; }
    public required string CorrelationId { get; init; }
    public required string EventType { get; init; }
    public required DateTime EventCreatedAtUtc { get; init; }
}
