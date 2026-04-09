using ComplaintClassifier.Application.Contracts;

namespace ComplaintClassifier.Application.Services;

public sealed class GuidComplaintIdGenerator : IComplaintIdGenerator
{
    public string NewId() => Guid.NewGuid().ToString("N");
}
