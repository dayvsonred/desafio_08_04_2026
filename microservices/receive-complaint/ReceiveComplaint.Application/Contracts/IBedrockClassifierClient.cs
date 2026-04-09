using ComplaintClassifier.Application.Models;

namespace ComplaintClassifier.Application.Contracts;

public interface IBedrockClassifierClient
{
    Task<BedrockClassificationOutput> ClassifyAsync(BedrockClassificationInput input, CancellationToken cancellationToken);
}
