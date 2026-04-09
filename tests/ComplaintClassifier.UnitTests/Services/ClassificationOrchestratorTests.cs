using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Models;
using ComplaintClassifier.Application.Options;
using ComplaintClassifier.Application.Services;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.UnitTests.Services;

public sealed class ClassificationOrchestratorTests
{
    [Fact]
    public async Task Classify_ShouldUseLlmFallback_WhenRulesRequireFallback()
    {
        var normalizer = new TextNormalizer();
        var classifier = new RuleBasedClassifier(
            Options.Create(new ClassificationOptions
            {
                MinimumWinningScore = 2,
                MinimumScoreGap = 1,
                LowConfidenceThreshold = 0.75,
                StrongCategoryRatio = 0.8,
                MaxStrongCategoriesBeforeLlm = 2
            }),
            normalizer);

        var fakeBedrock = new FakeBedrockClient(new BedrockClassificationOutput
        {
            CategoriaPrincipal = "aplicativo",
            CategoriasSecundarias = ["acesso"],
            Confianca = 0.89,
            Justificativa = "O texto descreve travamento do aplicativo e dificuldade de acesso."
        });

        var orchestrator = new ClassificationOrchestrator(classifier, fakeBedrock);
        var categories = new List<CategoryDefinition>
        {
            new()
            {
                Name = "aplicativo",
                Description = "...",
                Keywords = ["app", "aplicativo"]
            },
            new()
            {
                Name = "acesso",
                Description = "...",
                Keywords = ["acessar", "senha"]
            }
        };

        var outcome = await orchestrator.ClassifyAsync(
            "mensagem sem match para regras",
            normalizer.Normalize("mensagem sem match para regras"),
            categories,
            CancellationToken.None);

        Assert.True(outcome.UsedLlmFallback);
        Assert.Equal(DecisionSource.LLM, outcome.Result.DecisionSource);
        Assert.Equal("aplicativo", outcome.Result.PrimaryCategory);
        Assert.Contains("acesso", outcome.Result.SecondaryCategories);
    }

    private sealed class FakeBedrockClient : IBedrockClassifierClient
    {
        private readonly BedrockClassificationOutput _output;

        public FakeBedrockClient(BedrockClassificationOutput output)
        {
            _output = output;
        }

        public Task<BedrockClassificationOutput> ClassifyAsync(BedrockClassificationInput input, CancellationToken cancellationToken)
        {
            return Task.FromResult(_output);
        }
    }
}
