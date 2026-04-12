using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyComplaintMetrics.UnitTests.Handlers;

public sealed class UpdateDailyMetricsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldIncrementRepository_WithNormalizedEventType()
    {
        var repository = new FakeDailyMetricsRepository();
        var clock = new FixedClock(new DateTime(2026, 4, 9, 18, 30, 0, DateTimeKind.Utc));
        var handler = new UpdateDailyMetricsHandler(repository, clock, NullLogger<UpdateDailyMetricsHandler>.Instance);

        await handler.HandleAsync(new MetricsEventMessage
        {
            ComplaintId = "cmp-1",
            CorrelationId = "corr-1",
            EventType = "classified",
            CreatedAtUtc = new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc)
        }, CancellationToken.None);

        Assert.Equal("20260409", repository.LastDay);
        Assert.Equal("CLASSIFIED", repository.LastEventType);
        Assert.Equal("cmp-1", repository.LastIndexedComplaintId);
        Assert.Equal("corr-1", repository.LastIndexedCorrelationId);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenEventTypeIsInvalid()
    {
        var repository = new FakeDailyMetricsRepository();
        var clock = new FixedClock(new DateTime(2026, 4, 9, 18, 30, 0, DateTimeKind.Utc));
        var handler = new UpdateDailyMetricsHandler(repository, clock, NullLogger<UpdateDailyMetricsHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new MetricsEventMessage
        {
            ComplaintId = "cmp-1",
            CorrelationId = "corr-1",
            EventType = "invalid",
            CreatedAtUtc = new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc)
        }, CancellationToken.None));
    }

    private sealed class FakeDailyMetricsRepository : IDailyMetricsRepository
    {
        public string? LastDay { get; private set; }
        public string? LastEventType { get; private set; }
        public string? LastIndexedComplaintId { get; private set; }
        public string? LastIndexedCorrelationId { get; private set; }

        public Task IncrementAsync(string day, string eventType, DateTime updatedAtUtc, CancellationToken cancellationToken)
        {
            LastDay = day;
            LastEventType = eventType;
            return Task.CompletedTask;
        }

        public Task IndexMessageEventAsync(
            string day,
            string eventType,
            string complaintId,
            string correlationId,
            DateTime eventCreatedAtUtc,
            CancellationToken cancellationToken)
        {
            LastIndexedComplaintId = complaintId;
            LastIndexedCorrelationId = correlationId;
            return Task.CompletedTask;
        }

        public Task<DailyMetricsRecord?> GetByDayAsync(string day, CancellationToken cancellationToken)
            => Task.FromResult<DailyMetricsRecord?>(null);

        public Task<IReadOnlyList<DailyMetricMessageReference>> GetMessageEventsByDayAsync(
            string day,
            string eventType,
            int limit,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DailyMetricMessageReference>>([]);

        public Task<IReadOnlyList<DailyMetricMessageReference>> GetProcessedEventsByComplaintIdAsync(
            string complaintId,
            int limit,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DailyMetricMessageReference>>([]);

        public Task<IReadOnlyList<DailyMetricMessageReference>> GetProcessedEventsByCorrelationIdAsync(
            string correlationId,
            int limit,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DailyMetricMessageReference>>([]);
    }

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;

        public FixedClock(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        public DateTime UtcNow => _utcNow;
    }
}
