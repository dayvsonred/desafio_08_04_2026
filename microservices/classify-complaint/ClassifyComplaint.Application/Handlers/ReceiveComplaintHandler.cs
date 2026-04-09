using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Models;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;
using ComplaintClassifier.Domain.Messages;
using Microsoft.Extensions.Logging;

namespace ComplaintClassifier.Application.Handlers;

public sealed class ReceiveComplaintHandler
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IQueuePublisher _queuePublisher;
    private readonly IComplaintIdGenerator _complaintIdGenerator;
    private readonly IClock _clock;
    private readonly ILogger<ReceiveComplaintHandler> _logger;

    public ReceiveComplaintHandler(
        IComplaintRepository complaintRepository,
        IQueuePublisher queuePublisher,
        IComplaintIdGenerator complaintIdGenerator,
        IClock clock,
        ILogger<ReceiveComplaintHandler> logger)
    {
        _complaintRepository = complaintRepository;
        _queuePublisher = queuePublisher;
        _complaintIdGenerator = complaintIdGenerator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ReceiveComplaintResult> HandleAsync(string message, string? correlationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("O campo reclamacao é obrigatório.", nameof(message));
        }

        var complaintId = _complaintIdGenerator.NewId();
        var effectiveCorrelationId = string.IsNullOrWhiteSpace(correlationId) ? complaintId : correlationId;
        var now = _clock.UtcNow;

        var complaint = new ComplaintRecord
        {
            ComplaintId = complaintId,
            Message = message.Trim(),
            NormalizedMessage = null,
            Status = ComplaintStatus.RECEIVED,
            Classification = null,
            Error = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _complaintRepository.CreateAsync(complaint, cancellationToken);

        await _queuePublisher.PublishClassificationRequestedAsync(new QueueMessage
        {
            ComplaintId = complaintId,
            CorrelationId = effectiveCorrelationId,
            EventType = "COMPLAINT_RECEIVED",
            CreatedAtUtc = now
        }, cancellationToken);

        _logger.LogInformation("Complaint received and queued. complaintId={ComplaintId} correlationId={CorrelationId}", complaintId, effectiveCorrelationId);

        return new ReceiveComplaintResult(complaintId, effectiveCorrelationId, ComplaintStatus.RECEIVED);
    }
}
