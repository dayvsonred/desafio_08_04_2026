using ComplaintClassifier.Domain.Enums;

namespace ComplaintClassifier.Domain.Entities;

public sealed class ComplaintRecord
{
    public required string ComplaintId { get; init; }
    public string? Message { get; init; }
    public string? MessageReceivedS3Key { get; init; }
    public string? MessageProcessedS3Key { get; set; }
    public string? NormalizedMessage { get; set; }
    public ComplaintStatus Status { get; set; }
    public ClassificationResult? Classification { get; set; }
    public string? Error { get; set; }
    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime UpdatedAtUtc { get; set; }
}