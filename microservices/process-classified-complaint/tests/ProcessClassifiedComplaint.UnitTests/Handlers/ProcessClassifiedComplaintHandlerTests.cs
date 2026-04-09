using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace ProcessClassifiedComplaint.UnitTests.Handlers;

public sealed class ProcessClassifiedComplaintHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldSkip_WhenAlreadyProcessed()
    {
        var repository = new FakeComplaintRepository
        {
            CurrentComplaint = BuildComplaint("cmp-1", ComplaintStatus.PROCESSED)
        };

        var handler = new ProcessClassifiedComplaintHandler(
            repository,
            new FixedClock(new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc)),
            NullLogger<ProcessClassifiedComplaintHandler>.Instance);

        await handler.HandleAsync("cmp-1", "corr-1", CancellationToken.None);

        Assert.Empty(repository.StatusTransitions);
    }

    [Fact]
    public async Task HandleAsync_ShouldMoveToProcessingThenProcessed_WhenClassified()
    {
        var repository = new FakeComplaintRepository
        {
            CurrentComplaint = BuildComplaint("cmp-2", ComplaintStatus.CLASSIFIED),
            NextTryUpdateResult = true
        };

        var handler = new ProcessClassifiedComplaintHandler(
            repository,
            new FixedClock(new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc)),
            NullLogger<ProcessClassifiedComplaintHandler>.Instance);

        await handler.HandleAsync("cmp-2", "corr-2", CancellationToken.None);

        Assert.Equal(2, repository.StatusTransitions.Count);
        Assert.Equal(ComplaintStatus.PROCESSING, repository.StatusTransitions[0]);
        Assert.Equal(ComplaintStatus.PROCESSED, repository.StatusTransitions[1]);
    }

    private static ComplaintRecord BuildComplaint(string complaintId, ComplaintStatus status)
    {
        return new ComplaintRecord
        {
            ComplaintId = complaintId,
            Message = "mensagem",
            Status = status,
            CreatedAtUtc = new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Utc)
        };
    }

    private sealed class FakeComplaintRepository : IComplaintRepository
    {
        public ComplaintRecord? CurrentComplaint { get; set; }
        public bool NextTryUpdateResult { get; set; } = true;
        public List<ComplaintStatus> StatusTransitions { get; } = [];

        public Task CreateAsync(ComplaintRecord complaint, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<ComplaintRecord?> GetByIdAsync(string complaintId, CancellationToken cancellationToken)
            => Task.FromResult(CurrentComplaint);

        public Task<bool> TryUpdateStatusAsync(string complaintId, IReadOnlyCollection<ComplaintStatus> expectedStatuses, ComplaintStatus newStatus, DateTime updatedAtUtc, string? error, CancellationToken cancellationToken)
        {
            StatusTransitions.Add(newStatus);
            var result = NextTryUpdateResult;
            NextTryUpdateResult = true;
            return Task.FromResult(result);
        }

        public Task UpdateClassificationAsync(string complaintId, string normalizedMessage, ClassificationResult classification, ComplaintStatus status, DateTime updatedAtUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SetErrorAsync(string complaintId, ComplaintStatus failedStatus, string error, DateTime updatedAtUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _now;

        public FixedClock(DateTime now)
        {
            _now = now;
        }

        public DateTime UtcNow => _now;
    }
}
