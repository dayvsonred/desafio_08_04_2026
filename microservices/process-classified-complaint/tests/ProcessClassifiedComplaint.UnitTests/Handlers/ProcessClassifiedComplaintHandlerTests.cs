using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;
using ComplaintClassifier.Domain.Messages;
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
            new FakeComplaintMessageStorage(),
            new FakeQueuePublisher(),
            new FixedClock(new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc)),
            NullLogger<ProcessClassifiedComplaintHandler>.Instance);

        await handler.HandleAsync("cmp-1", "corr-1", "msg-1", CancellationToken.None);

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

        var storage = new FakeComplaintMessageStorage();
        var queuePublisher = new FakeQueuePublisher();
        var handler = new ProcessClassifiedComplaintHandler(
            repository,
            storage,
            queuePublisher,
            new FixedClock(new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc)),
            NullLogger<ProcessClassifiedComplaintHandler>.Instance);

        await handler.HandleAsync("cmp-2", "corr-2", "msg-2", CancellationToken.None);

        Assert.Equal(2, repository.StatusTransitions.Count);
        Assert.Equal(ComplaintStatus.PROCESSING, repository.StatusTransitions[0]);
        Assert.Equal(ComplaintStatus.PROCESSED, repository.StatusTransitions[1]);
        Assert.Equal("complaint_message_processed/20260409/cmp-2_msg-2.json", repository.ProcessedMessageS3Key);
        Assert.Equal("mensagem original", storage.LoadedMessage);
        Assert.NotNull(queuePublisher.LastMetricsEvent);
        Assert.Equal("PROCESSED", queuePublisher.LastMetricsEvent!.EventType);
    }

    private static ComplaintRecord BuildComplaint(string complaintId, ComplaintStatus status)
    {
        return new ComplaintRecord
        {
            ComplaintId = complaintId,
            Message = null,
            MessageReceivedS3Key = $"complaint_message_received/20260409/{complaintId}.json",
            MessageProcessedS3Key = null,
            Status = status,
            Classification = new ClassificationResult
            {
                PrimaryCategory = "aplicativo",
                SecondaryCategories = ["acesso"],
                Confidence = 0.91,
                DecisionSource = DecisionSource.RULES,
                Justification = "Teste",
                ScoreBreakdown = []
            },
            CreatedAtUtc = new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Utc)
        };
    }

    private sealed class FakeComplaintRepository : IComplaintRepository
    {
        public ComplaintRecord? CurrentComplaint { get; set; }
        public bool NextTryUpdateResult { get; set; } = true;
        public List<ComplaintStatus> StatusTransitions { get; } = [];
        public string? ProcessedMessageS3Key { get; private set; }

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

        public Task SetProcessedMessagePathAsync(string complaintId, string messageProcessedS3Key, DateTime updatedAtUtc, CancellationToken cancellationToken)
        {
            ProcessedMessageS3Key = messageProcessedS3Key;
            return Task.CompletedTask;
        }

        public Task SetErrorAsync(string complaintId, ComplaintStatus failedStatus, string error, DateTime updatedAtUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeComplaintMessageStorage : IComplaintMessageStorage
    {
        public string? LoadedMessage { get; private set; }

        public Task<string> SaveReceivedMessageAsync(string complaintId, string correlationId, string message, DateTime receivedAtUtc, CancellationToken cancellationToken)
            => Task.FromResult(string.Empty);

        public Task<string> LoadReceivedMessageAsync(string messageReceivedS3Key, CancellationToken cancellationToken)
        {
            LoadedMessage = "mensagem original";
            return Task.FromResult(LoadedMessage);
        }

        public Task<string> SaveProcessedMessageAsync(string complaintId, string correlationId, string message, ClassificationResult classification, string? messageId, DateTime processedAtUtc, CancellationToken cancellationToken)
            => Task.FromResult($"complaint_message_processed/{processedAtUtc:yyyyMMdd}/{complaintId}_{messageId}.json");
    }

    private sealed class FakeQueuePublisher : IQueuePublisher
    {
        public MetricsEventMessage? LastMetricsEvent { get; private set; }

        public Task PublishClassificationRequestedAsync(QueueMessage message, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishProcessingRequestedAsync(QueueMessage message, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishMetricsEventAsync(MetricsEventMessage message, CancellationToken cancellationToken)
        {
            LastMetricsEvent = message;
            return Task.CompletedTask;
        }
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
