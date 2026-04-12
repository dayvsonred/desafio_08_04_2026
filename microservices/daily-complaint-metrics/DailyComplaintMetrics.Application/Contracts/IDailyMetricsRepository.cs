using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Contracts;

public interface IDailyMetricsRepository
{
    Task IncrementAsync(string day, string eventType, DateTime updatedAtUtc, CancellationToken cancellationToken);
    Task IndexMessageEventAsync(
        string day,
        string eventType,
        string complaintId,
        string correlationId,
        DateTime eventCreatedAtUtc,
        CancellationToken cancellationToken);
    Task<DailyMetricsRecord?> GetByDayAsync(string day, CancellationToken cancellationToken);
    Task<IReadOnlyList<DailyMetricMessageReference>> GetMessageEventsByDayAsync(
        string day,
        string eventType,
        int limit,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<DailyMetricMessageReference>> GetProcessedEventsByComplaintIdAsync(
        string complaintId,
        int limit,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<DailyMetricMessageReference>> GetProcessedEventsByCorrelationIdAsync(
        string correlationId,
        int limit,
        CancellationToken cancellationToken);
}
