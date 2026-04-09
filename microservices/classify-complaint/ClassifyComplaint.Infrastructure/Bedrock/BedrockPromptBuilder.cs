using System.Text.Json;
using ComplaintClassifier.Application.Models;

namespace ComplaintClassifier.Infrastructure.Bedrock;

public static class BedrockPromptBuilder
{
    public static string Build(BedrockClassificationInput input)
    {
        var categoriesJson = JsonSerializer.Serialize(input.Categories, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return "Voce e um classificador deterministico de reclamacoes bancarias.\n\n" +
               "Objetivo:\n" +
               "- Classificar a reclamacao em exatamente 1 categoria principal.\n" +
               "- Opcionalmente sugerir categorias secundarias.\n\n" +
               "Restricoes obrigatorias:\n" +
               "- Responda APENAS JSON valido.\n" +
               "- Nao use markdown, explicacoes ou texto adicional.\n" +
               "- Use somente categorias da lista informada.\n" +
               "- confianca deve ser numero entre 0 e 1.\n" +
               "- justificativa curta (uma frase).\n\n" +
               "Formato obrigatorio:\n" +
               "{\n" +
               "  \"categoriaPrincipal\": \"string\",\n" +
               "  \"categoriasSecundarias\": [\"string\"],\n" +
               "  \"confianca\": 0.0,\n" +
               "  \"justificativa\": \"string\"\n" +
               "}\n\n" +
               "Categorias disponiveis:\n" +
               categoriesJson +
               "\n\nTexto original:\n" +
               input.OriginalMessage +
               "\n\nTexto normalizado:\n" +
               input.NormalizedMessage;
    }
}
