using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Handlers;

public sealed class GetMetricMessageEventsHandler
{
    private static readonly HashSet<string> SupportedEventTypes = new(StringComparer.Ordinal)
    {
        "RECEIVED",
        "PROCESSED"
    };

    private readonly IDailyMetricsRepository _dailyMetricsRepository;

    public GetMetricMessageEventsHandler(IDailyMetricsRepository dailyMetricsRepository)
    {
        _dailyMetricsRepository = dailyMetricsRepository;
    }

    public async Task<IReadOnlyList<DailyMetricMessageReference>> HandleAsync(
        string day,
        string eventType,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedEventType = eventType.Trim().ToUpperInvariant();
        if (!SupportedEventTypes.Contains(normalizedEventType))
        {
            throw new InvalidOperationException($"Tipo de evento nao suportado para listagem de mensagens: {eventType}");
        }

        return await _dailyMetricsRepository.GetMessageEventsByDayAsync(
            day,
            normalizedEventType,
            Math.Clamp(limit, 1, 500),
            cancellationToken);
    }
}
