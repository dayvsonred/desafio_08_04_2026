using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;
using ComplaintClassifier.Domain.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReceiveComplaint.UnitTests.Handlers;

public sealed class ReceiveComplaintHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenMessageIsEmpty()
    {
        var repository = new FakeComplaintRepository();
        var queuePublisher = new FakeQueuePublisher();
        var handler = new ReceiveComplaintHandler(
            repository,
            queuePublisher,
            new FakeComplaintIdGenerator("cmp-1"),
            new FixedClock(new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc)),
            NullLogger<ReceiveComplaintHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync("   ", null, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldPersistAndPublish_WhenValidMessage()
    {
        var repository = new FakeComplaintRepository();
        var queuePublisher = new FakeQueuePublisher();
        var now = new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc);
        var handler = new ReceiveComplaintHandler(
            repository,
            queuePublisher,
            new FakeComplaintIdGenerator("cmp-123"),
            new FixedClock(now),
            NullLogger<ReceiveComplaintHandler>.Instance);

        var result = await handler.HandleAsync("Mensagem de reclamacao", "corr-1", CancellationToken.None);

        Assert.Equal("cmp-123", result.ComplaintId);
        Assert.Equal(ComplaintStatus.RECEIVED, result.Status);

        Assert.NotNull(repository.CreatedComplaint);
        Assert.Equal("cmp-123", repository.CreatedComplaint!.ComplaintId);
        Assert.Equal(ComplaintStatus.RECEIVED, repository.CreatedComplaint.Status);

        Assert.NotNull(queuePublisher.ClassificationMessage);
        Assert.Equal("cmp-123", queuePublisher.ClassificationMessage!.ComplaintId);
        Assert.Equal("corr-1", queuePublisher.ClassificationMessage.CorrelationId);
    }

    private sealed class FakeComplaintRepository : IComplaintRepository
    {
        public ComplaintRecord? CreatedComplaint { get; private set; }

        public Task CreateAsync(ComplaintRecord complaint, CancellationToken cancellationToken)
        {
            CreatedComplaint = complaint;
            return Task.CompletedTask;
        }

        public Task<ComplaintRecord?> GetByIdAsync(string complaintId, CancellationToken cancellationToken)
            => Task.FromResult<ComplaintRecord?>(null);

        public Task<bool> TryUpdateStatusAsync(string complaintId, IReadOnlyCollection<ComplaintStatus> expectedStatuses, ComplaintStatus newStatus, DateTime updatedAtUtc, string? error, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task UpdateClassificationAsync(string complaintId, string normalizedMessage, ClassificationResult classification, ComplaintStatus status, DateTime updatedAtUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SetErrorAsync(string complaintId, ComplaintStatus failedStatus, string error, DateTime updatedAtUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeQueuePublisher : IQueuePublisher
    {
        public QueueMessage? ClassificationMessage { get; private set; }

        public Task PublishClassificationRequestedAsync(QueueMessage message, CancellationToken cancellationToken)
        {
            ClassificationMessage = message;
            return Task.CompletedTask;
        }

        public Task PublishProcessingRequestedAsync(QueueMessage message, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeComplaintIdGenerator : IComplaintIdGenerator
    {
        private readonly string _id;

        public FakeComplaintIdGenerator(string id)
        {
            _id = id;
        }

        public string NewId() => _id;
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
