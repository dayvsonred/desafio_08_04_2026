using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Contracts;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryDefinition>> GetAllAsync(CancellationToken cancellationToken);
}
