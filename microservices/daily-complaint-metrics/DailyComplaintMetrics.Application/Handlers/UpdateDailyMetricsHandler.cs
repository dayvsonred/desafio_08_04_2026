using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Messages;
using Microsoft.Extensions.Logging;

namespace ComplaintClassifier.Application.Handlers;

public sealed class UpdateDailyMetricsHandler
{
    private readonly IDailyMetricsRepository _dailyMetricsRepository;
    private readonly IClock _clock;
    private readonly ILogger<UpdateDailyMetricsHandler> _logger;

    public UpdateDailyMetricsHandler(
        IDailyMetricsRepository dailyMetricsRepository,
        IClock clock,
        ILogger<UpdateDailyMetricsHandler> logger)
    {
        _dailyMetricsRepository = dailyMetricsRepository;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(MetricsEventMessage message, CancellationToken cancellationToken)
    {
        var day = message.CreatedAtUtc.ToString("yyyyMMdd");
        var normalizedEventType = message.EventType.Trim().ToUpperInvariant();

        ValidateEventType(normalizedEventType);

        await _dailyMetricsRepository.IncrementAsync(day, normalizedEventType, _clock.UtcNow, cancellationToken);

        _logger.LogInformation(
            "Daily metric updated. day={Day} eventType={EventType} complaintId={ComplaintId} correlationId={CorrelationId}",
            day,
            normalizedEventType,
            message.ComplaintId,
            message.CorrelationId);
    }

    private static void ValidateEventType(string eventType)
    {
        if (eventType is not ("RECEIVED" or "CLASSIFIED" or "CLASSIFICATION_FAILED" or "PROCESSED" or "PROCESSING_FAILED"))
        {
            throw new InvalidOperationException($"Tipo de evento de metrica invalido: {eventType}");
        }
    }
}
