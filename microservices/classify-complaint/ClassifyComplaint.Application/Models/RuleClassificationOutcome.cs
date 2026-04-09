using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Models;

public sealed class RuleClassificationOutcome
{
    public required bool RequiresLlm { get; init; }
    public required string FallbackReason { get; init; }
    public string? PrimaryCategory { get; init; }
    public IReadOnlyList<string> SecondaryCategories { get; init; } = Array.Empty<string>();
    public double Confidence { get; init; }
    public string Justification { get; init; } = string.Empty;
    public IReadOnlyList<CategoryScore> ScoreBreakdown { get; init; } = Array.Empty<CategoryScore>();
}
