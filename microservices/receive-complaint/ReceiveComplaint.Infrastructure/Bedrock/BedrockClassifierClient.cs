using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Models;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Infrastructure.Bedrock;

public sealed class BedrockClassifierClient : IBedrockClassifierClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAmazonBedrockRuntime _bedrockRuntime;
    private readonly AwsResourceOptions _options;
    private readonly ILogger<BedrockClassifierClient> _logger;

    public BedrockClassifierClient(
        IAmazonBedrockRuntime bedrockRuntime,
        IOptions<AwsResourceOptions> options,
        ILogger<BedrockClassifierClient> logger)
    {
        _bedrockRuntime = bedrockRuntime;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BedrockClassificationOutput> ClassifyAsync(BedrockClassificationInput input, CancellationToken cancellationToken)
    {
        var prompt = BedrockPromptBuilder.Build(input);

        var payload = JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 300,
            temperature = 0,
            top_p = 1,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = prompt
                        }
                    }
                }
            }
        });

        var response = await _bedrockRuntime.InvokeModelAsync(new InvokeModelRequest
        {
            ModelId = _options.BedrockModelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(payload))
        }, cancellationToken);

        using var reader = new StreamReader(response.Body);
        var rawResponse = await reader.ReadToEndAsync(cancellationToken);

        var modelText = ExtractModelText(rawResponse);
        var jsonPayload = ExtractJsonPayload(modelText);

        var result = JsonSerializer.Deserialize<BedrockClassificationOutput>(jsonPayload, JsonSerializerOptions);
        if (result is null)
        {
            throw new InvalidOperationException("Bedrock retornou payload invalido para classificacao.");
        }

        _logger.LogInformation("Bedrock fallback applied. primaryCategory={PrimaryCategory} confidence={Confidence}", result.CategoriaPrincipal, result.Confianca);

        return result;
    }

    private static string ExtractModelText(string rawResponse)
    {
        using var document = JsonDocument.Parse(rawResponse);
        var root = document.RootElement;

        if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Resposta do Bedrock sem campo content.");
        }

        foreach (var item in content.EnumerateArray())
        {
            if (!item.TryGetProperty("text", out var text))
            {
                continue;
            }

            var output = text.GetString();
            if (!string.IsNullOrWhiteSpace(output))
            {
                return output;
            }
        }

        throw new InvalidOperationException("Resposta do Bedrock sem texto valido.");
    }

    private static string ExtractJsonPayload(string modelText)
    {
        var start = modelText.IndexOf('{');
        var end = modelText.LastIndexOf('}');

        if (start < 0 || end < 0 || end <= start)
        {
            throw new InvalidOperationException("Bedrock nao retornou JSON valido.");
        }

        return modelText[start..(end + 1)];
    }
}
