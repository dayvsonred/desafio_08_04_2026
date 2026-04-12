using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Domain.Entities;

namespace DailyComplaintMetrics.UnitTests.Handlers;

public sealed class GetMetricMessageEventsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldNormalizeEventType_AndCallRepository()
    {
        var repository = new FakeDailyMetricsRepository();
        var handler = new GetMetricMessageEventsHandler(repository);

        await handler.HandleAsync("20260412", "processed", 25, CancellationToken.None);

        Assert.Equal("20260412", repository.LastDay);
        Assert.Equal("PROCESSED", repository.LastEventType);
        Assert.Equal(25, repository.LastLimit);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenEventTypeIsNotSupported()
    {
        var repository = new FakeDailyMetricsRepository();
        var handler = new GetMetricMessageEventsHandler(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync("20260412", "CLASSIFIED", 10, CancellationToken.None));
    }

    private sealed class FakeDailyMetricsRepository : IDailyMetricsRepository
    {
        public string? LastDay { get; private set; }
        public string? LastEventType { get; private set; }
        public int LastLimit { get; private set; }

        public Task IncrementAsync(string day, string eventType, DateTime updatedAtUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task IndexMessageEventAsync(
            string day,
            string eventType,
            string complaintId,
            string correlationId,
            DateTime eventCreatedAtUtc,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<DailyMetricsRecord?> GetByDayAsync(string day, CancellationToken cancellationToken)
            => Task.FromResult<DailyMetricsRecord?>(null);

        public Task<IReadOnlyList<DailyMetricMessageReference>> GetMessageEventsByDayAsync(
            string day,
            string eventType,
            int limit,
            CancellationToken cancellationToken)
        {
            LastDay = day;
            LastEventType = eventType;
            LastLimit = limit;
            return Task.FromResult<IReadOnlyList<DailyMetricMessageReference>>([]);
        }

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
}
