using ComplaintClassifier.Application.Contracts;

namespace ComplaintClassifier.Application.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
