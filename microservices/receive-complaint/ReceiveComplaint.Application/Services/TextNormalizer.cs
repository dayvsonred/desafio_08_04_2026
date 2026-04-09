using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ComplaintClassifier.Application.Contracts;

namespace ComplaintClassifier.Application.Services;

public sealed class TextNormalizer : ITextNormalizer
{
    private static readonly Regex NonAlphaNumericRegex = new("[^a-z0-9\\s]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex MultiSpaceRegex = new("\\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var lower = input.Trim().ToLowerInvariant();
        var withoutAccents = RemoveAccents(lower);
        var withoutPunctuation = NonAlphaNumericRegex.Replace(withoutAccents, " ");
        var collapsed = MultiSpaceRegex.Replace(withoutPunctuation, " ").Trim();

        return collapsed;
    }

    private static string RemoveAccents(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
