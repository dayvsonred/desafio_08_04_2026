using ComplaintClassifier.Application.Services;

namespace ClassifyComplaint.UnitTests.Services;

public sealed class TextNormalizerTests
{
    [Fact]
    public void Normalize_ShouldLowercaseRemoveAccentsPunctuationAndCollapseSpaces()
    {
        var normalizer = new TextNormalizer();

        var result = normalizer.Normalize("  Aplicativo está TRAVANDO!!! Não   consigo acessar.  ");

        Assert.Equal("aplicativo esta travando nao consigo acessar", result);
    }
}
