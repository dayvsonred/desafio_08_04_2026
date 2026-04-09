namespace ComplaintClassifier.Application.Contracts;

public interface IClock
{
    DateTime UtcNow { get; }
}
