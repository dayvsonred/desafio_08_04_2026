namespace ComplaintClassifier.Application.Models;

public sealed class BedrockClassificationOutput
{
    public required string CategoriaPrincipal { get; init; }
    public required IReadOnlyList<string> CategoriasSecundarias { get; init; }
    public required double Confianca { get; init; }
    public required string Justificativa { get; init; }
}
