using ComplaintClassifier.Domain.Entities;

namespace ComplaintClassifier.Application.Contracts;

public interface IComplaintMessageStorage
{
    Task<string> SaveReceivedMessageAsync(
        string complaintId,
        string correlationId,
        string message,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken);

    Task<string> LoadReceivedMessageAsync(string messageReceivedS3Key, CancellationToken cancellationToken);

    Task<string> SaveProcessedMessageAsync(
        string complaintId,
        string correlationId,
        string message,
        ClassificationResult classification,
        string? messageId,
        DateTime processedAtUtc,
        CancellationToken cancellationToken);
}