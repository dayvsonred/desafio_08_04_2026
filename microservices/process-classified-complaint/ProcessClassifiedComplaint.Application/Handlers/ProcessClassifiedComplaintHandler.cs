using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Enums;
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
    private readonly IClock _clock;
    private readonly ILogger<ProcessClassifiedComplaintHandler> _logger;

    public ProcessClassifiedComplaintHandler(
        IComplaintRepository complaintRepository,
        IClock clock,
        ILogger<ProcessClassifiedComplaintHandler> logger)
    {
        _complaintRepository = complaintRepository;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(string complaintId, string? correlationId, CancellationToken cancellationToken)
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
            _logger.LogInformation("Processing classified complaint. complaintId={ComplaintId} correlationId={CorrelationId}", complaintId, effectiveCorrelationId);

            await _complaintRepository.TryUpdateStatusAsync(
                complaintId,
                AllowedStatusesToProcessed,
                ComplaintStatus.PROCESSED,
                _clock.UtcNow,
                null,
                cancellationToken);

            _logger.LogInformation("Complaint marked as PROCESSED. complaintId={ComplaintId} correlationId={CorrelationId}", complaintId, effectiveCorrelationId);
        }
        catch (Exception exception)
        {
            await _complaintRepository.SetErrorAsync(
                complaintId,
                ComplaintStatus.PROCESSING_FAILED,
                exception.Message,
                _clock.UtcNow,
                cancellationToken);

            _logger.LogError(exception, "Processing failed. complaintId={ComplaintId} correlationId={CorrelationId}", complaintId, effectiveCorrelationId);
            throw;
        }
    }
}
