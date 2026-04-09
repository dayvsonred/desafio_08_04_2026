namespace ComplaintClassifier.Application.Models;

public sealed class BedrockClassificationInput
{
    public required string OriginalMessage { get; init; }
    public required string NormalizedMessage { get; init; }
    public required IReadOnlyList<BedrockCategoryDefinition> Categories { get; init; }
}

public sealed class BedrockCategoryDefinition
{
    public required string Nome { get; init; }
    public required string Descricao { get; init; }
    public required IReadOnlyList<string> PalavrasChave { get; init; }
}
