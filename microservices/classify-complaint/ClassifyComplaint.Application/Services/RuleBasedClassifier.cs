using System.Text.RegularExpressions;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Models;
using ComplaintClassifier.Application.Options;
using ComplaintClassifier.Application.Utilities;
using ComplaintClassifier.Domain.Entities;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Application.Services;

public sealed class RuleBasedClassifier : IRuleBasedClassifier
{
    private readonly ClassificationOptions _options;
    private readonly ITextNormalizer _normalizer;

    public RuleBasedClassifier(IOptions<ClassificationOptions> options, ITextNormalizer normalizer)
    {
        _options = options.Value;
        _normalizer = normalizer;
    }

    public RuleClassificationOutcome Classify(string normalizedMessage, IReadOnlyList<CategoryDefinition> categories)
    {
        var scores = categories
            .Select(category => BuildCategoryScore(normalizedMessage, category))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Category, StringComparer.Ordinal)
            .ToList();

        var winner = scores.FirstOrDefault();
        var second = scores.Skip(1).FirstOrDefault();

        if (winner is null || winner.Score == 0)
        {
            return BuildFallback("Nenhuma categoria teve correspondência por palavra-chave.", scores);
        }

        if (second is not null && second.Score == winner.Score && winner.Score > 0)
        {
            return BuildFallback("Empate entre as categorias mais pontuadas.", scores);
        }

        if (winner.Score < _options.MinimumWinningScore)
        {
            return BuildFallback($"Score máximo abaixo do mínimo ({_options.MinimumWinningScore}).", scores);
        }

        if (second is not null && second.Score > 0)
        {
            var scoreGap = winner.Score - second.Score;
            if (scoreGap < _options.MinimumScoreGap)
            {
                return BuildFallback($"Diferença de score insuficiente ({scoreGap}).", scores);
            }
        }

        var strongCategories = scores
            .Where(item => item.Score > 0 && item.Score >= winner.Score * _options.StrongCategoryRatio)
            .ToList();

        if (strongCategories.Count > _options.MaxStrongCategoriesBeforeLlm)
        {
            return BuildFallback("Múltiplas categorias fortes indicam ambiguidade.", scores);
        }

        var totalScore = scores.Sum(item => item.Score);
        var secondScore = second?.Score ?? 0;
        var confidence = ConfidenceCalculator.Compute(winner.Score, secondScore, totalScore);

        if (confidence < _options.LowConfidenceThreshold)
        {
            return BuildFallback($"Confiança das regras abaixo do limiar ({_options.LowConfidenceThreshold:F2}).", scores);
        }

        var secondaryCategories = scores
            .Where(item => item.Score > 0 && !string.Equals(item.Category, winner.Category, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Category)
            .ToList();

        var justification = BuildRuleJustification(winner, secondaryCategories);

        return new RuleClassificationOutcome
        {
            RequiresLlm = false,
            FallbackReason = string.Empty,
            PrimaryCategory = winner.Category,
            SecondaryCategories = secondaryCategories,
            Confidence = confidence,
            Justification = justification,
            ScoreBreakdown = scores
        };
    }

    private CategoryScore BuildCategoryScore(string normalizedMessage, CategoryDefinition category)
    {
        var matchedKeywords = new List<string>();
        var score = 0;

        foreach (var keyword in category.Keywords)
        {
            var normalizedKeyword = _normalizer.Normalize(keyword);
            if (string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                continue;
            }

            if (!ContainsKeyword(normalizedMessage, normalizedKeyword))
            {
                continue;
            }

            matchedKeywords.Add(keyword);
            score += normalizedKeyword.Contains(' ') ? 2 : 1;
        }

        return new CategoryScore
        {
            Category = category.Name,
            Score = score,
            MatchedKeywords = matchedKeywords
        };
    }

    private static bool ContainsKeyword(string normalizedMessage, string normalizedKeyword)
    {
        var pattern = $"\\b{Regex.Escape(normalizedKeyword).Replace("\\ ", "\\s+")}\\b";
        return Regex.IsMatch(normalizedMessage, pattern, RegexOptions.CultureInvariant);
    }

    private static RuleClassificationOutcome BuildFallback(string reason, IReadOnlyList<CategoryScore> scores)
    {
        return new RuleClassificationOutcome
        {
            RequiresLlm = true,
            FallbackReason = reason,
            ScoreBreakdown = scores,
            Confidence = 0,
            Justification = "Regras insuficientes para decisão final."
        };
    }

    private static string BuildRuleJustification(CategoryScore winner, IReadOnlyList<string> secondaryCategories)
    {
        var primaryKeywords = winner.MatchedKeywords.Count == 0
            ? "sem palavras-chave explícitas"
            : string.Join(", ", winner.MatchedKeywords);

        if (secondaryCategories.Count == 0)
        {
            return $"Categoria '{winner.Category}' definida pelas palavras-chave: {primaryKeywords}.";
        }

        return $"Categoria principal '{winner.Category}' definida por {primaryKeywords}; categorias secundárias: {string.Join(", ", secondaryCategories)}.";
    }
}
