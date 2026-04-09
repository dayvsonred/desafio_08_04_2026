using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Contracts;

public interface IDailyMetricsRepository
{
    Task IncrementAsync(string day, string eventType, DateTime updatedAtUtc, CancellationToken cancellationToken);
    Task<DailyMetricsRecord?> GetByDayAsync(string day, CancellationToken cancellationToken);
}
