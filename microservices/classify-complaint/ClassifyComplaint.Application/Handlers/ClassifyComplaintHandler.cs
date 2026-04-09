using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Enums;
using ComplaintClassifier.Domain.Messages;
using Microsoft.Extensions.Logging;

namespace ComplaintClassifier.Application.Handlers;

public sealed class ClassifyComplaintHandler
{
    private static readonly ComplaintStatus[] AllowedStatusesToClassify =
    [
        ComplaintStatus.RECEIVED,
        ComplaintStatus.CLASSIFICATION_FAILED
    ];

    private readonly IComplaintRepository _complaintRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IClassificationOrchestrator _classificationOrchestrator;
    private readonly ITextNormalizer _textNormalizer;
    private readonly IComplaintMessageStorage _messageStorage;
    private readonly IQueuePublisher _queuePublisher;
    private readonly IClock _clock;
    private readonly ILogger<ClassifyComplaintHandler> _logger;

    public ClassifyComplaintHandler(
        IComplaintRepository complaintRepository,
        ICategoryRepository categoryRepository,
        IClassificationOrchestrator classificationOrchestrator,
        ITextNormalizer textNormalizer,
        IComplaintMessageStorage messageStorage,
        IQueuePublisher queuePublisher,
        IClock clock,
        ILogger<ClassifyComplaintHandler> logger)
    {
        _complaintRepository = complaintRepository;
        _categoryRepository = categoryRepository;
        _classificationOrchestrator = classificationOrchestrator;
        _textNormalizer = textNormalizer;
        _messageStorage = messageStorage;
        _queuePublisher = queuePublisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(string complaintId, string? correlationId, CancellationToken cancellationToken)
    {
        var effectiveCorrelationId = string.IsNullOrWhiteSpace(correlationId) ? complaintId : correlationId;
        var complaint = await _complaintRepository.GetByIdAsync(complaintId, cancellationToken)
            ?? throw new InvalidOperationException($"Reclamação não encontrada: {complaintId}");

        if (complaint.Status is ComplaintStatus.CLASSIFIED or ComplaintStatus.PROCESSING or ComplaintStatus.PROCESSED)
        {
            _logger.LogInformation("Skipping already classified complaint. complaintId={ComplaintId} status={Status} correlationId={CorrelationId}", complaintId, complaint.Status, effectiveCorrelationId);
            return;
        }

        var now = _clock.UtcNow;
        var statusMoved = await _complaintRepository.TryUpdateStatusAsync(
            complaintId,
            AllowedStatusesToClassify,
            ComplaintStatus.CLASSIFYING,
            now,
            null,
            cancellationToken);

        if (!statusMoved)
        {
            _logger.LogInformation("Status transition to CLASSIFYING ignored. complaintId={ComplaintId} currentStatus={Status} correlationId={CorrelationId}", complaintId, complaint.Status, effectiveCorrelationId);
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(complaint.MessageReceivedS3Key))
            {
                throw new InvalidOperationException($"Reclamacao sem caminho da mensagem no S3: {complaintId}");
            }

            var message = await _messageStorage.LoadReceivedMessageAsync(complaint.MessageReceivedS3Key, cancellationToken);
            var normalizedMessage = _textNormalizer.Normalize(message);
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);

            if (categories.Count == 0)
            {
                throw new InvalidOperationException("Nenhuma categoria cadastrada no DynamoDB.");
            }

            var classificationOutcome = await _classificationOrchestrator.ClassifyAsync(
                message,
                normalizedMessage,
                categories,
                cancellationToken);

            await _complaintRepository.UpdateClassificationAsync(
                complaintId,
                normalizedMessage,
                classificationOutcome.Result,
                ComplaintStatus.CLASSIFIED,
                _clock.UtcNow,
                cancellationToken);

            await _queuePublisher.PublishProcessingRequestedAsync(new QueueMessage
            {
                ComplaintId = complaintId,
                CorrelationId = effectiveCorrelationId,
                EventType = "COMPLAINT_CLASSIFIED",
                CreatedAtUtc = _clock.UtcNow
            }, cancellationToken);

            _logger.LogInformation(
                "Complaint classified. complaintId={ComplaintId} correlationId={CorrelationId} source={DecisionSource} fallbackReason={FallbackReason}",
                complaintId,
                effectiveCorrelationId,
                classificationOutcome.Result.DecisionSource,
                classificationOutcome.FallbackReason);
        }
        catch (Exception exception)
        {
            await _complaintRepository.SetErrorAsync(
                complaintId,
                ComplaintStatus.CLASSIFICATION_FAILED,
                exception.Message,
                _clock.UtcNow,
                cancellationToken);

            _logger.LogError(exception, "Classification failed. complaintId={ComplaintId} correlationId={CorrelationId}", complaintId, effectiveCorrelationId);
            throw;
        }
    }
}