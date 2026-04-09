using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Handlers;

public sealed class GetDailyMetricsHandler
{
    private readonly IDailyMetricsRepository _dailyMetricsRepository;

    public GetDailyMetricsHandler(IDailyMetricsRepository dailyMetricsRepository)
    {
        _dailyMetricsRepository = dailyMetricsRepository;
    }

    public async Task<DailyMetricsRecord> HandleAsync(string day, CancellationToken cancellationToken)
    {
        var metrics = await _dailyMetricsRepository.GetByDayAsync(day, cancellationToken);

        if (metrics is not null)
        {
            return metrics;
        }

        var now = DateTime.UtcNow;
        return new DailyMetricsRecord
        {
            Day = day,
            ReceivedCount = 0,
            ClassifiedCount = 0,
            ClassificationFailedCount = 0,
            ProcessedSuccessCount = 0,
            ProcessedErrorCount = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}
