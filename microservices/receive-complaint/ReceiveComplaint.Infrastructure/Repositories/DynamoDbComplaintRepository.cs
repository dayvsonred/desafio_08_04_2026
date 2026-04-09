using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Infrastructure.Repositories;

public sealed class DynamoDbComplaintRepository : IComplaintRepository
{
    private const string MetadataSk = "METADATA";

    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly AwsResourceOptions _options;
    private readonly ILogger<DynamoDbComplaintRepository> _logger;

    public DynamoDbComplaintRepository(
        IAmazonDynamoDB dynamoDb,
        IOptions<AwsResourceOptions> options,
        ILogger<DynamoDbComplaintRepository> logger)
    {
        _dynamoDb = dynamoDb;
        _options = options.Value;
        _logger = logger;
    }

    public async Task CreateAsync(ComplaintRecord complaint, CancellationToken cancellationToken)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = BuildPk(complaint.ComplaintId) },
            ["SK"] = new AttributeValue { S = MetadataSk },
            ["complaintId"] = new AttributeValue { S = complaint.ComplaintId },
            ["status"] = new AttributeValue { S = complaint.Status.ToString() },
            ["createdAt"] = new AttributeValue { S = complaint.CreatedAtUtc.ToString("O") },
            ["updatedAt"] = new AttributeValue { S = complaint.UpdatedAtUtc.ToString("O") }
        };

        if (!string.IsNullOrWhiteSpace(complaint.Message))
        {
            item["message"] = new AttributeValue { S = complaint.Message };
        }

        if (!string.IsNullOrWhiteSpace(complaint.MessageReceivedS3Key))
        {
            item["messageReceivedS3Key"] = new AttributeValue { S = complaint.MessageReceivedS3Key };
        }

        if (!string.IsNullOrWhiteSpace(complaint.MessageProcessedS3Key))
        {
            item["messageProcessedS3Key"] = new AttributeValue { S = complaint.MessageProcessedS3Key };
        }

        var request = new PutItemRequest
        {
            TableName = _options.ComplaintsTableName,
            ConditionExpression = "attribute_not_exists(PK)",
            Item = item
        };

        await _dynamoDb.PutItemAsync(request, cancellationToken);
    }

    public async Task<ComplaintRecord?> GetByIdAsync(string complaintId, CancellationToken cancellationToken)
    {
        var request = new GetItemRequest
        {
            TableName = _options.ComplaintsTableName,
            Key = BuildComplaintKey(complaintId),
            ConsistentRead = true
        };

        var response = await _dynamoDb.GetItemAsync(request, cancellationToken);
        return response.Item is { Count: > 0 } ? MapComplaint(response.Item) : null;
    }

    public async Task<bool> TryUpdateStatusAsync(
        string complaintId,
        IReadOnlyCollection<ComplaintStatus> expectedStatuses,
        ComplaintStatus newStatus,
        DateTime updatedAtUtc,
        string? error,
        CancellationToken cancellationToken)
    {
        var expressionValues = new Dictionary<string, AttributeValue>
        {
            [":newStatus"] = new AttributeValue { S = newStatus.ToString() },
            [":updatedAt"] = new AttributeValue { S = updatedAtUtc.ToString("O") }
        };

        var conditions = new List<string>();
        var index = 0;
        foreach (var expectedStatus in expectedStatuses)
        {
            var key = $":expected{index}";
            expressionValues[key] = new AttributeValue { S = expectedStatus.ToString() };
            conditions.Add($"#status = {key}");
            index++;
        }

        var conditionExpression = conditions.Count == 0
            ? "attribute_exists(PK)"
            : $"attribute_exists(PK) AND ({string.Join(" OR ", conditions)})";

        var updateExpression = "SET #status = :newStatus, #updatedAt = :updatedAt";

        if (error is not null)
        {
            expressionValues[":error"] = new AttributeValue { S = error };
            updateExpression += ", #error = :error";
        }
        else
        {
            updateExpression += " REMOVE #error";
        }

        try
        {
            await _dynamoDb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _options.ComplaintsTableName,
                Key = BuildComplaintKey(complaintId),
                UpdateExpression = updateExpression,
                ConditionExpression = conditionExpression,
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#status"] = "status",
                    ["#updatedAt"] = "updatedAt",
                    ["#error"] = "error"
                },
                ExpressionAttributeValues = expressionValues
            }, cancellationToken);

            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task UpdateClassificationAsync(
        string complaintId,
        string normalizedMessage,
        ClassificationResult classification,
        ComplaintStatus status,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var classificationMap = new Dictionary<string, AttributeValue>
        {
            ["primaryCategory"] = new AttributeValue { S = classification.PrimaryCategory },
            ["secondaryCategories"] = new AttributeValue { L = classification.SecondaryCategories.Select(category => new AttributeValue { S = category }).ToList() },
            ["confidence"] = new AttributeValue { N = classification.Confidence.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
            ["decisionSource"] = new AttributeValue { S = classification.DecisionSource.ToString() },
            ["justification"] = new AttributeValue { S = classification.Justification },
            ["scoreBreakdown"] = new AttributeValue
            {
                L = classification.ScoreBreakdown.Select(score => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        ["category"] = new AttributeValue { S = score.Category },
                        ["score"] = new AttributeValue { N = score.Score.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                        ["matchedKeywords"] = new AttributeValue { L = score.MatchedKeywords.Select(keyword => new AttributeValue { S = keyword }).ToList() }
                    }
                }).ToList()
            }
        };

        await _dynamoDb.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.ComplaintsTableName,
            Key = BuildComplaintKey(complaintId),
            UpdateExpression = "SET #normalizedMessage = :normalizedMessage, #status = :status, #classification = :classification, #updatedAt = :updatedAt REMOVE #error",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#normalizedMessage"] = "normalizedMessage",
                ["#status"] = "status",
                ["#classification"] = "classification",
                ["#updatedAt"] = "updatedAt",
                ["#error"] = "error"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":normalizedMessage"] = new AttributeValue { S = normalizedMessage },
                [":status"] = new AttributeValue { S = status.ToString() },
                [":classification"] = new AttributeValue { M = classificationMap },
                [":updatedAt"] = new AttributeValue { S = updatedAtUtc.ToString("O") }
            }
        }, cancellationToken);
    }

    public async Task SetProcessedMessagePathAsync(
        string complaintId,
        string messageProcessedS3Key,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await _dynamoDb.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.ComplaintsTableName,
            Key = BuildComplaintKey(complaintId),
            UpdateExpression = "SET #messageProcessedS3Key = :messageProcessedS3Key, #updatedAt = :updatedAt",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#messageProcessedS3Key"] = "messageProcessedS3Key",
                ["#updatedAt"] = "updatedAt"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":messageProcessedS3Key"] = new AttributeValue { S = messageProcessedS3Key },
                [":updatedAt"] = new AttributeValue { S = updatedAtUtc.ToString("O") }
            }
        }, cancellationToken);
    }

    public async Task SetErrorAsync(
        string complaintId,
        ComplaintStatus failedStatus,
        string error,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await _dynamoDb.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.ComplaintsTableName,
            Key = BuildComplaintKey(complaintId),
            UpdateExpression = "SET #status = :status, #error = :error, #updatedAt = :updatedAt",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status",
                ["#error"] = "error",
                ["#updatedAt"] = "updatedAt"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":status"] = new AttributeValue { S = failedStatus.ToString() },
                [":error"] = new AttributeValue { S = error },
                [":updatedAt"] = new AttributeValue { S = updatedAtUtc.ToString("O") }
            }
        }, cancellationToken);

        _logger.LogWarning("Complaint error persisted. complaintId={ComplaintId} failedStatus={FailedStatus}", complaintId, failedStatus);
    }

    private static ComplaintRecord MapComplaint(Dictionary<string, AttributeValue> item)
    {
        return new ComplaintRecord
        {
            ComplaintId = item["complaintId"].S,
            Message = item.TryGetValue("message", out var messageValue) ? messageValue.S : null,
            MessageReceivedS3Key = item.TryGetValue("messageReceivedS3Key", out var messageReceivedS3Key) ? messageReceivedS3Key.S : null,
            MessageProcessedS3Key = item.TryGetValue("messageProcessedS3Key", out var messageProcessedS3Key) ? messageProcessedS3Key.S : null,
            NormalizedMessage = item.TryGetValue("normalizedMessage", out var normalized) ? normalized.S : null,
            Status = Enum.Parse<ComplaintStatus>(item["status"].S, true),
            Classification = item.TryGetValue("classification", out var classification) && classification.M is { Count: > 0 }
                ? MapClassification(classification.M)
                : null,
            Error = item.TryGetValue("error", out var error) ? error.S : null,
            CreatedAtUtc = DateTime.Parse(item["createdAt"].S, null, System.Globalization.DateTimeStyles.RoundtripKind),
            UpdatedAtUtc = DateTime.Parse(item["updatedAt"].S, null, System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }

    private static ClassificationResult MapClassification(Dictionary<string, AttributeValue> map)
    {
        var scoreBreakdown = map["scoreBreakdown"].L.Select(scoreItem => new CategoryScore
        {
            Category = scoreItem.M["category"].S,
            Score = int.Parse(scoreItem.M["score"].N, System.Globalization.CultureInfo.InvariantCulture),
            MatchedKeywords = scoreItem.M["matchedKeywords"].L.Select(keyword => keyword.S).ToList()
        }).ToList();

        return new ClassificationResult
        {
            PrimaryCategory = map["primaryCategory"].S,
            SecondaryCategories = map["secondaryCategories"].L.Select(category => category.S).ToList(),
            Confidence = double.Parse(map["confidence"].N, System.Globalization.CultureInfo.InvariantCulture),
            DecisionSource = Enum.Parse<DecisionSource>(map["decisionSource"].S, true),
            Justification = map["justification"].S,
            ScoreBreakdown = scoreBreakdown
        };
    }

    private static Dictionary<string, AttributeValue> BuildComplaintKey(string complaintId)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = BuildPk(complaintId) },
            ["SK"] = new AttributeValue { S = MetadataSk }
        };
    }

    private static string BuildPk(string complaintId) => $"COMPLAINT#{complaintId}";
}