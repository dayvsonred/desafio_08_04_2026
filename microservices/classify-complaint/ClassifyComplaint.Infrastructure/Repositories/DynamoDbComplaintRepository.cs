using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Domain.Enums;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

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
        var adjustments = new List<string>();
        var safeNormalizedMessage = EnsureNonEmpty(normalizedMessage, "texto nao normalizado", "normalizedMessage", adjustments);
        var safePrimaryCategory = EnsureNonEmpty(classification.PrimaryCategory, "nao-classificada", "classification.primaryCategory", adjustments);
        var safeSecondaryCategories = SanitizeStringList(classification.SecondaryCategories, "classification.secondaryCategories", adjustments);
        var safeJustification = EnsureNonEmpty(classification.Justification, "Classificacao gerada sem justificativa.", "classification.justification", adjustments);
        var safeConfidence = SanitizeConfidence(classification.Confidence, adjustments);
        var safeScoreBreakdown = SanitizeScoreBreakdown(classification.ScoreBreakdown, adjustments);
        var secondaryCategoriesAttribute = BuildStringListAttribute(safeSecondaryCategories, "classification.secondaryCategories", adjustments);
        var scoreBreakdownAttribute = BuildScoreBreakdownAttribute(safeScoreBreakdown, adjustments);

        var classificationMap = new Dictionary<string, AttributeValue>
        {
            ["primaryCategory"] = new AttributeValue { S = safePrimaryCategory },
            ["secondaryCategories"] = secondaryCategoriesAttribute,
            ["confidence"] = new AttributeValue { N = safeConfidence.ToString("F2", CultureInfo.InvariantCulture) },
            ["decisionSource"] = new AttributeValue { S = classification.DecisionSource.ToString() },
            ["justification"] = new AttributeValue { S = safeJustification },
            ["scoreBreakdown"] = scoreBreakdownAttribute
        };
        var classificationDiagnostics = DescribeAttributeValue(new AttributeValue { M = classificationMap });

        _logger.LogInformation(
            "Persisting complaint classification. complaintId={ComplaintId} status={Status} decisionSource={DecisionSource} primaryCategory={PrimaryCategory} secondaryCategoriesCount={SecondaryCategoriesCount} scoreBreakdownCount={ScoreBreakdownCount} confidence={Confidence}",
            complaintId,
            status,
            classification.DecisionSource,
            safePrimaryCategory,
            safeSecondaryCategories.Count,
            safeScoreBreakdown.Count,
            safeConfidence);

        if (adjustments.Count > 0)
        {
            _logger.LogWarning(
                "Classification payload sanitized before persistence. complaintId={ComplaintId} adjustments={Adjustments}",
                complaintId,
                string.Join("; ", adjustments));
        }

        try
        {
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
                    [":normalizedMessage"] = new AttributeValue { S = safeNormalizedMessage },
                    [":status"] = new AttributeValue { S = status.ToString() },
                    [":classification"] = new AttributeValue { M = classificationMap },
                    [":updatedAt"] = new AttributeValue { S = updatedAtUtc.ToString("O") }
                }
            }, cancellationToken);
        }
        catch (AmazonDynamoDBException exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist complaint classification. complaintId={ComplaintId} status={Status} decisionSource={DecisionSource} primaryCategory={PrimaryCategory} secondaryCategoriesCount={SecondaryCategoriesCount} scoreBreakdownCount={ScoreBreakdownCount} confidence={Confidence} classificationDiagnostics={ClassificationDiagnostics} adjustments={Adjustments}",
                complaintId,
                status,
                classification.DecisionSource,
                safePrimaryCategory,
                safeSecondaryCategories.Count,
                safeScoreBreakdown.Count,
                safeConfidence,
                classificationDiagnostics,
                adjustments.Count == 0 ? "none" : string.Join("; ", adjustments));

            throw;
        }
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
        var scoreBreakdown = ReadScoreBreakdown(map, "scoreBreakdown");

        return new ClassificationResult
        {
            PrimaryCategory = map["primaryCategory"].S,
            SecondaryCategories = ReadStringList(map, "secondaryCategories"),
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

    private static string EnsureNonEmpty(string? value, string fallback, string fieldName, List<string> adjustments)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            adjustments.Add($"{fieldName}:fallback");
            return fallback;
        }

        return value.Trim();
    }

    private static List<string> SanitizeStringList(IEnumerable<string>? values, string fieldName, List<string> adjustments)
    {
        if (values is null)
        {
            adjustments.Add($"{fieldName}:null-to-empty-list");
            return [];
        }

        var sanitizedValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return sanitizedValues;
    }

    private static double SanitizeConfidence(double confidence, List<string> adjustments)
    {
        if (double.IsNaN(confidence) || double.IsInfinity(confidence))
        {
            adjustments.Add("classification.confidence:invalid-number-to-zero");
            return 0;
        }

        var clamped = Math.Round(Math.Clamp(confidence, 0.0, 1.0), 2);
        if (!clamped.Equals(confidence))
        {
            adjustments.Add("classification.confidence:clamped");
        }

        return clamped;
    }

    private static List<CategoryScore> SanitizeScoreBreakdown(IReadOnlyList<CategoryScore>? scores, List<string> adjustments)
    {
        if (scores is null)
        {
            adjustments.Add("classification.scoreBreakdown:null-to-empty-list");
            return [];
        }

        var sanitizedScores = new List<CategoryScore>(scores.Count);

        for (var index = 0; index < scores.Count; index++)
        {
            var score = scores[index];
            if (score is null)
            {
                adjustments.Add($"classification.scoreBreakdown[{index}]:null-entry-ignored");
                continue;
            }

            var category = EnsureNonEmpty(score.Category, $"categoria_{index}", $"classification.scoreBreakdown[{index}].category", adjustments);
            var matchedKeywords = SanitizeStringList(score.MatchedKeywords, $"classification.scoreBreakdown[{index}].matchedKeywords", adjustments);

            sanitizedScores.Add(new CategoryScore
            {
                Category = category,
                Score = score.Score,
                MatchedKeywords = matchedKeywords
            });
        }

        return sanitizedScores;
    }

    private static AttributeValue BuildStringListAttribute(IReadOnlyList<string> values, string fieldName, List<string> adjustments)
    {
        if (values.Count == 0)
        {
            adjustments.Add($"{fieldName}:empty-list-to-null");
            return new AttributeValue { NULL = true };
        }

        return new AttributeValue
        {
            L = values.Select(value => new AttributeValue { S = value }).ToList()
        };
    }

    private static AttributeValue BuildScoreBreakdownAttribute(IReadOnlyList<CategoryScore> scores, List<string> adjustments)
    {
        if (scores.Count == 0)
        {
            adjustments.Add("classification.scoreBreakdown:empty-list-to-null");
            return new AttributeValue { NULL = true };
        }

        var entries = scores.Select((score, index) => new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                ["category"] = new AttributeValue { S = score.Category },
                ["score"] = new AttributeValue { N = score.Score.ToString(CultureInfo.InvariantCulture) },
                ["matchedKeywords"] = BuildStringListAttribute(score.MatchedKeywords, $"classification.scoreBreakdown[{index}].matchedKeywords", adjustments)
            }
        }).ToList();

        return new AttributeValue { L = entries };
    }

    private static IReadOnlyList<string> ReadStringList(IReadOnlyDictionary<string, AttributeValue> map, string key)
    {
        if (!map.TryGetValue(key, out var attribute) || attribute is null || attribute.NULL || attribute.L is null)
        {
            return [];
        }

        return attribute.L
            .Where(item => item?.S is not null)
            .Select(item => item.S)
            .ToList();
    }

    private static IReadOnlyList<CategoryScore> ReadScoreBreakdown(IReadOnlyDictionary<string, AttributeValue> map, string key)
    {
        if (!map.TryGetValue(key, out var attribute) || attribute is null || attribute.NULL || attribute.L is null)
        {
            return [];
        }

        var scores = new List<CategoryScore>(attribute.L.Count);
        foreach (var scoreItem in attribute.L)
        {
            if (scoreItem?.M is null)
            {
                continue;
            }

            if (!scoreItem.M.TryGetValue("category", out var categoryAttribute) || categoryAttribute.S is null)
            {
                continue;
            }

            if (!scoreItem.M.TryGetValue("score", out var scoreAttribute) || scoreAttribute.N is null)
            {
                continue;
            }

            var matchedKeywords = ReadStringList(scoreItem.M, "matchedKeywords");

            scores.Add(new CategoryScore
            {
                Category = categoryAttribute.S,
                Score = int.Parse(scoreAttribute.N, CultureInfo.InvariantCulture),
                MatchedKeywords = matchedKeywords
            });
        }

        return scores;
    }

    private static string DescribeAttributeValue(AttributeValue? value, int depth = 0)
    {
        if (value is null)
        {
            return "null";
        }

        if (value.S is not null)
        {
            return $"S(len={value.S.Length})";
        }

        if (value.N is not null)
        {
            return $"N({value.N})";
        }

        if (value.NULL)
        {
            return "NULL";
        }

        if (value.L is { } list)
        {
            if (depth >= 2)
            {
                return $"L(count={list.Count})";
            }

            var items = list.Take(3).Select(item => DescribeAttributeValue(item, depth + 1)).ToList();
            if (list.Count > 3)
            {
                items.Add("...");
            }

            return $"L(count={list.Count})[{string.Join(", ", items)}]";
        }

        if (value.M is { } map)
        {
            if (depth >= 2)
            {
                return $"M(count={map.Count})";
            }

            var parts = map
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => $"{entry.Key}:{DescribeAttributeValue(entry.Value, depth + 1)}");

            return $"M(count={map.Count}){{{string.Join(", ", parts)}}}";
        }

        return "EMPTY";
    }

    private static string BuildPk(string complaintId) => $"COMPLAINT#{complaintId}";
}
