using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Enums;
using ComplaintClassifier.Domain.Messages;
using Microsoft.Extensions.Logging;

namespace ComplaintClassifier.Application.Handlers;

public sealed class ProcessClassifiedComplaintHandler
{
    private static readonly ComplaintStatus[] AllowedStatusesToProcessing =
    [
        ComplaintStatus.CLASSIFIED,
        ComplaintStatus.PROCESSING_FAILED
    ];

    private static readonly ComplaintStatus[] AllowedStatusesToProcessed =
    [
        ComplaintStatus.PROCESSING
    ];

    private readonly IComplaintRepository _complaintRepository;
    private readonly IComplaintMessageStorage _messageStorage;
    private readonly IQueuePublisher _queuePublisher;
    private readonly IClock _clock;
    private readonly ILogger<ProcessClassifiedComplaintHandler> _logger;

    public ProcessClassifiedComplaintHandler(
        IComplaintRepository complaintRepository,
        IComplaintMessageStorage messageStorage,
        IQueuePublisher queuePublisher,
        IClock clock,
        ILogger<ProcessClassifiedComplaintHandler> logger)
    {
        _complaintRepository = complaintRepository;
        _messageStorage = messageStorage;
        _queuePublisher = queuePublisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(string complaintId, string? correlationId, string? messageId, CancellationToken cancellationToken)
    {
        var effectiveCorrelationId = string.IsNullOrWhiteSpace(correlationId) ? complaintId : correlationId;
        var complaint = await _complaintRepository.GetByIdAsync(complaintId, cancellationToken)
            ?? throw new InvalidOperationException($"Reclamação não encontrada: {complaintId}");

        if (complaint.Status == ComplaintStatus.PROCESSED)
        {
            _logger.LogInformation("Complaint already processed. complaintId={ComplaintId} correlationId={CorrelationId}", complaintId, effectiveCorrelationId);
            return;
        }

        var now = _clock.UtcNow;
        var movedToProcessing = await _complaintRepository.TryUpdateStatusAsync(
            complaintId,
            AllowedStatusesToProcessing,
            ComplaintStatus.PROCESSING,
            now,
            null,
            cancellationToken);

        if (!movedToProcessing && complaint.Status != ComplaintStatus.PROCESSING)
        {
            _logger.LogInformation("Skipping processing due to status. complaintId={ComplaintId} status={Status} correlationId={CorrelationId}", complaintId, complaint.Status, effectiveCorrelationId);
            return;
        }

        try
        {
            if (complaint.Classification is null)
            {
                throw new InvalidOperationException($"Reclamacao sem classificacao para processamento: {complaintId}");
            }

            if (string.IsNullOrWhiteSpace(complaint.MessageReceivedS3Key))
            {
                throw new InvalidOperationException($"Reclamacao sem caminho da mensagem no S3: {complaintId}");
            }

            var message = await _messageStorage.LoadReceivedMessageAsync(complaint.MessageReceivedS3Key, cancellationToken);
            var processedAtUtc = _clock.UtcNow;
            var processedS3Key = await _messageStorage.SaveProcessedMessageAsync(
                complaintId,
                effectiveCorrelationId,
                message,
                complaint.Classification,
                messageId,
                processedAtUtc,
                cancellationToken);

            await _complaintRepository.SetProcessedMessagePathAsync(
                complaintId,
                processedS3Key,
                processedAtUtc,
                cancellationToken);

            await _complaintRepository.TryUpdateStatusAsync(
                complaintId,
                AllowedStatusesToProcessed,
                ComplaintStatus.PROCESSED,
                _clock.UtcNow,
                null,
                cancellationToken);

            await _queuePublisher.PublishMetricsEventAsync(new MetricsEventMessage
            {
                ComplaintId = complaintId,
                CorrelationId = effectiveCorrelationId,
                EventType = "PROCESSED",
                CreatedAtUtc = _clock.UtcNow
            }, cancellationToken);

            _logger.LogInformation(
                "Complaint marked as PROCESSED. complaintId={ComplaintId} correlationId={CorrelationId} messageId={MessageId} processedS3Key={ProcessedS3Key}",
                complaintId,
                effectiveCorrelationId,
                messageId,
                processedS3Key);
        }
        catch (Exception exception)
        {
            await _complaintRepository.SetErrorAsync(
                complaintId,
                ComplaintStatus.PROCESSING_FAILED,
                exception.Message,
                _clock.UtcNow,
                cancellationToken);

            try
            {
                await _queuePublisher.PublishMetricsEventAsync(new MetricsEventMessage
                {
                    ComplaintId = complaintId,
                    CorrelationId = effectiveCorrelationId,
                    EventType = "PROCESSING_FAILED",
                    CreatedAtUtc = _clock.UtcNow
                }, cancellationToken);
            }
            catch (Exception metricsException)
            {
                _logger.LogWarning(metricsException, "Failed to publish metrics event. complaintId={ComplaintId} correlationId={CorrelationId}", complaintId, effectiveCorrelationId);
            }

            _logger.LogError(exception, "Processing failed. complaintId={ComplaintId} correlationId={CorrelationId} messageId={MessageId}", complaintId, effectiveCorrelationId, messageId);
            throw;
        }
    }
}
