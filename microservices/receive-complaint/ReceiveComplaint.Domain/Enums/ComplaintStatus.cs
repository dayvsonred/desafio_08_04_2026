namespace ComplaintClassifier.Domain.Enums;

public enum ComplaintStatus
{
    RECEIVED = 0,
    CLASSIFYING = 1,
    CLASSIFIED = 2,
    CLASSIFICATION_FAILED = 3,
    PROCESSING = 4,
    PROCESSED = 5,
    PROCESSING_FAILED = 6
}
