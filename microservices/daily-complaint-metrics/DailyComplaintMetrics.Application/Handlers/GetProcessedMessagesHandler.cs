using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Handlers;

public sealed class GetProcessedMessagesHandler
{
    private readonly IDailyMetricsRepository _dailyMetricsRepository;

    public GetProcessedMessagesHandler(IDailyMetricsRepository dailyMetricsRepository)
    {
        _dailyMetricsRepository = dailyMetricsRepository;
    }

    public Task<IReadOnlyList<DailyMetricMessageReference>> HandleByComplaintIdAsync(
        string complaintId,
        int limit,
        CancellationToken cancellationToken)
    {
        return _dailyMetricsRepository.GetProcessedEventsByComplaintIdAsync(
            complaintId,
            Math.Clamp(limit, 1, 500),
            cancellationToken);
    }

    public Task<IReadOnlyList<DailyMetricMessageReference>> HandleByCorrelationIdAsync(
        string correlationId,
        int limit,
        CancellationToken cancellationToken)
    {
        return _dailyMetricsRepository.GetProcessedEventsByCorrelationIdAsync(
            correlationId,
            Math.Clamp(limit, 1, 500),
            cancellationToken);
    }
}
