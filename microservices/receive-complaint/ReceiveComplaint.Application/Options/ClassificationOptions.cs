namespace ComplaintClassifier.Application.Options;

public sealed class ClassificationOptions
{
    public const string SectionName = "Classification";

    public int MinimumWinningScore { get; init; } = 2;
    public int MinimumScoreGap { get; init; } = 1;
    public double LowConfidenceThreshold { get; init; } = 0.75;
    public double StrongCategoryRatio { get; init; } = 0.8;
    public int MaxStrongCategoriesBeforeLlm { get; init; } = 2;
}
