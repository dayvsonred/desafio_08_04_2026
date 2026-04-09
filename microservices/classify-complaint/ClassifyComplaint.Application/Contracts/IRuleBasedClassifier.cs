using ComplaintClassifier.Application.Models;
using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Contracts;

public interface IRuleBasedClassifier
{
    RuleClassificationOutcome Classify(string normalizedMessage, IReadOnlyList<CategoryDefinition> categories);
}
