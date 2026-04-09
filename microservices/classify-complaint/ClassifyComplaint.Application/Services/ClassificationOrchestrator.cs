using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Models;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;

namespace ComplaintClassifier.Application.Services;

public sealed class ClassificationOrchestrator : IClassificationOrchestrator
{
    private readonly IRuleBasedClassifier _ruleBasedClassifier;
    private readonly IBedrockClassifierClient _bedrockClassifierClient;

    public ClassificationOrchestrator(IRuleBasedClassifier ruleBasedClassifier, IBedrockClassifierClient bedrockClassifierClient)
    {
        _ruleBasedClassifier = ruleBasedClassifier;
        _bedrockClassifierClient = bedrockClassifierClient;
    }

    public async Task<ClassificationOutcome> ClassifyAsync(
        string originalMessage,
        string normalizedMessage,
        IReadOnlyList<CategoryDefinition> categories,
        CancellationToken cancellationToken)
    {
        var ruleOutcome = _ruleBasedClassifier.Classify(normalizedMessage, categories);

        if (!ruleOutcome.RequiresLlm)
        {
            var ruleResult = new ClassificationResult
            {
                PrimaryCategory = ruleOutcome.PrimaryCategory!,
                SecondaryCategories = ruleOutcome.SecondaryCategories,
                Confidence = ruleOutcome.Confidence,
                DecisionSource = DecisionSource.RULES,
                Justification = ruleOutcome.Justification,
                ScoreBreakdown = ruleOutcome.ScoreBreakdown
            };

            return new ClassificationOutcome
            {
                Result = ruleResult,
                UsedLlmFallback = false,
                FallbackReason = null
            };
        }

        var llmInput = new BedrockClassificationInput
        {
            OriginalMessage = originalMessage,
            NormalizedMessage = normalizedMessage,
            Categories = categories.Select(category => new BedrockCategoryDefinition
            {
                Nome = category.Name,
                Descricao = category.Description,
                PalavrasChave = category.Keywords
            }).ToList()
        };

        var llmOutput = await _bedrockClassifierClient.ClassifyAsync(llmInput, cancellationToken);
        var secondaryCategories = llmOutput.CategoriasSecundarias
            .Where(category => !string.Equals(category, llmOutput.CategoriaPrincipal, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var llmResult = new ClassificationResult
        {
            PrimaryCategory = llmOutput.CategoriaPrincipal,
            SecondaryCategories = secondaryCategories,
            Confidence = Math.Round(Math.Clamp(llmOutput.Confianca, 0.0, 1.0), 2),
            DecisionSource = DecisionSource.LLM,
            Justification = llmOutput.Justificativa,
            ScoreBreakdown = ruleOutcome.ScoreBreakdown
        };

        return new ClassificationOutcome
        {
            Result = llmResult,
            UsedLlmFallback = true,
            FallbackReason = ruleOutcome.FallbackReason
        };
    }
}
