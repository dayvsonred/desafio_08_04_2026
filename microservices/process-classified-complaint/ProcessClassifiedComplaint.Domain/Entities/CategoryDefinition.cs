namespace ComplaintClassifier.Domain.Entities;

public sealed class CategoryDefinition
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Keywords { get; init; }
    public required string Description { get; init; }
}
