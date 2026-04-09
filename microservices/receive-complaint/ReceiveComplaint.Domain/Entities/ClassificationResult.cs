using ComplaintClassifier.Domain.Enums;

namespace ComplaintClassifier.Domain.Entities;

public sealed class ClassificationResult
{
    public required string PrimaryCategory { get; init; }
    public required IReadOnlyList<string> SecondaryCategories { get; init; }
    public required double Confidence { get; init; }
    public required DecisionSource DecisionSource { get; init; }
    public required string Justification { get; init; }
    public required IReadOnlyList<CategoryScore> ScoreBreakdown { get; init; }
}
