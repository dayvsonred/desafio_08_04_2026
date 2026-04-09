using ComplaintClassifier.Application.Models;
using ComplaintClassifier.Application.Options;
using ComplaintClassifier.Application.Services;
using ComplaintClassifier.Domain.Entities;
using Microsoft.Extensions.Options;

namespace ClassifyComplaint.UnitTests.Services;

public sealed class RuleBasedClassifierTests
{
    private readonly TextNormalizer _normalizer = new();

    [Fact]
    public void Classify_ShouldCalculateScoreAndPickWinnerCategory()
    {
        var classifier = CreateClassifier();
        var categories = BuildCategories();
        var message = _normalizer.Normalize("Estou com problemas para acessar minha conta e o aplicativo esta travando muito.");

        var result = classifier.Classify(message, categories);

        Assert.False(result.RequiresLlm);
        Assert.Equal("aplicativo", result.PrimaryCategory);
        Assert.Contains("acesso", result.SecondaryCategories);

        var appScore = result.ScoreBreakdown.First(item => item.Category == "aplicativo");
        var accessScore = result.ScoreBreakdown.First(item => item.Category == "acesso");

        Assert.Equal(2, appScore.Score);
        Assert.Equal(1, accessScore.Score);
        Assert.Contains("aplicativo", appScore.MatchedKeywords);
        Assert.Contains("travando", appScore.MatchedKeywords);
    }

    [Fact]
    public void Classify_ShouldRequestLlmWhenNoCategoryMatches()
    {
        var classifier = CreateClassifier();
        var categories = BuildCategories();
        var message = _normalizer.Normalize("Quero elogiar o atendimento da agencia.");

        var result = classifier.Classify(message, categories);

        Assert.True(result.RequiresLlm);
        Assert.Contains("Nenhuma categoria", result.FallbackReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Classify_ShouldRequestLlmWhenTieOccurs()
    {
        var classifier = CreateClassifier();
        var categories = new List<CategoryDefinition>
        {
            new()
            {
                Name = "cobranca",
                Description = "...",
                Keywords = ["fatura", "indevido"]
            },
            new()
            {
                Name = "fraude",
                Description = "...",
                Keywords = ["fatura", "fraude"]
            }
        };

        var message = _normalizer.Normalize("Minha fatura apresenta problema.");
        var result = classifier.Classify(message, categories);

        Assert.True(result.RequiresLlm);
        Assert.Contains("Empate", result.FallbackReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Classify_ShouldRequestLlmWhenMultipleStrongCategories()
    {
        var classifier = CreateClassifier(new ClassificationOptions
        {
            MinimumWinningScore = 2,
            MinimumScoreGap = 1,
            LowConfidenceThreshold = 0.10,
            StrongCategoryRatio = 0.60,
            MaxStrongCategoriesBeforeLlm = 2
        });

        var categories = new List<CategoryDefinition>
        {
            new()
            {
                Name = "aplicativo",
                Description = "...",
                Keywords = ["aplicativo", "travando", "erro"]
            },
            new()
            {
                Name = "acesso",
                Description = "...",
                Keywords = ["acessar", "senha"]
            },
            new()
            {
                Name = "cobranca",
                Description = "...",
                Keywords = ["fatura", "cobranca"]
            }
        };

        var message = _normalizer.Normalize("Aplicativo travando com erro, nao consigo acessar senha e minha fatura teve cobranca indevida.");
        var result = classifier.Classify(message, categories);

        Assert.True(result.RequiresLlm);
        Assert.Contains("categorias fortes", result.FallbackReason, StringComparison.OrdinalIgnoreCase);
    }

    private RuleBasedClassifier CreateClassifier(ClassificationOptions? options = null)
    {
        return new RuleBasedClassifier(
            Options.Create(options ?? new ClassificationOptions
            {
                MinimumWinningScore = 2,
                MinimumScoreGap = 1,
                LowConfidenceThreshold = 0.75,
                StrongCategoryRatio = 0.8,
                MaxStrongCategoriesBeforeLlm = 2
            }),
            _normalizer);
    }

    private static List<CategoryDefinition> BuildCategories()
    {
        return
        [
            new CategoryDefinition
            {
                Name = "acesso",
                Description = "...",
                Keywords = ["acessar", "login", "senha"]
            },
            new CategoryDefinition
            {
                Name = "aplicativo",
                Description = "...",
                Keywords = ["app", "aplicativo", "travando", "erro"]
            }
        ];
    }
}
