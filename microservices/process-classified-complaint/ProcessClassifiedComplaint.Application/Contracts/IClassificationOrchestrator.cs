using ComplaintClassifier.Application.Models;
using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Contracts;

public interface IClassificationOrchestrator
{
    Task<ClassificationOutcome> ClassifyAsync(
        string originalMessage,
        string normalizedMessage,
        IReadOnlyList<CategoryDefinition> categories,
        CancellationToken cancellationToken);
}
