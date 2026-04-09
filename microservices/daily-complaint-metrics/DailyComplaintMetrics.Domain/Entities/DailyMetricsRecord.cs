namespace ComplaintClassifier.Domain.Entities;

public sealed class DailyMetricsRecord
{
    public required string Day { get; init; }
    public required int ReceivedCount { get; init; }
    public required int ClassifiedCount { get; init; }
    public required int ClassificationFailedCount { get; init; }
    public required int ProcessedSuccessCount { get; init; }
    public required int ProcessedErrorCount { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
}
