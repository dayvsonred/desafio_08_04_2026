namespace ComplaintClassifier.Domain.Entities;

public sealed class CategoryScore
{
    public required string Category { get; init; }
    public required int Score { get; init; }
    public required IReadOnlyList<string> MatchedKeywords { get; init; }
}
