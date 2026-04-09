using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Models;

public sealed class ClassificationOutcome
{
    public required ClassificationResult Result { get; init; }
    public required bool UsedLlmFallback { get; init; }
    public string? FallbackReason { get; init; }
}
