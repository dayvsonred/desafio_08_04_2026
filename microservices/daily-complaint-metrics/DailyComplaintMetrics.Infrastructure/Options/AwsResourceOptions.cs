namespace ComplaintClassifier.Infrastructure.Options;

public sealed class AwsResourceOptions
{
    public const string SectionName = "AwsResources";

    public string DailyMetricsTableName { get; init; } = "daily-metrics";
}
