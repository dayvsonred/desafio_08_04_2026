namespace ComplaintClassifier.Infrastructure.Options;

public sealed class AwsResourceOptions
{
    public const string SectionName = "AwsResources";

    public string ComplaintsTableName { get; init; } = "complaints";
    public string CategoriesTableName { get; init; } = "categories";
    public string ClassificationQueueUrl { get; init; } = string.Empty;
    public string ProcessingQueueUrl { get; init; } = string.Empty;
    public string BedrockModelId { get; init; } = "anthropic.claude-3-haiku-20240307-v1:0";
}
