using ComplaintClassifier.Domain.Enums;

namespace ComplaintClassifier.Application.Models;

public sealed record ReceiveComplaintResult(
    string ComplaintId,
    string CorrelationId,
    ComplaintStatus Status);
