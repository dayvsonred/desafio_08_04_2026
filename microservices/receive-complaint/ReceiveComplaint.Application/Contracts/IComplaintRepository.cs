using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;

namespace ComplaintClassifier.Application.Contracts;

public interface IComplaintRepository
{
    Task CreateAsync(ComplaintRecord complaint, CancellationToken cancellationToken);
    Task<ComplaintRecord?> GetByIdAsync(string complaintId, CancellationToken cancellationToken);
    Task<bool> TryUpdateStatusAsync(
        string complaintId,
        IReadOnlyCollection<ComplaintStatus> expectedStatuses,
        ComplaintStatus newStatus,
        DateTime updatedAtUtc,
        string? error,
        CancellationToken cancellationToken);
    Task UpdateClassificationAsync(
        string complaintId,
        string normalizedMessage,
        ClassificationResult classification,
        ComplaintStatus status,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken);
    Task SetProcessedMessagePathAsync(
        string complaintId,
        string messageProcessedS3Key,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken);
    Task SetErrorAsync(
        string complaintId,
        ComplaintStatus failedStatus,
        string error,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken);
}