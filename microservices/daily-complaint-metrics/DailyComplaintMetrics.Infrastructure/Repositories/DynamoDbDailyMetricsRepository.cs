using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Infrastructure.Repositories;

public sealed class DynamoDbDailyMetricsRepository : IDailyMetricsRepository
{
    private const string MetadataSk = "METADATA";

    private static readonly IReadOnlyDictionary<string, string> CounterAttributeByEventType = new Dictionary<string, string>
    {
        ["RECEIVED"] = "receivedCount",
        ["CLASSIFIED"] = "classifiedCount",
        ["CLASSIFICATION_FAILED"] = "classificationFailedCount",
        ["PROCESSED"] = "processedSuccessCount",
        ["PROCESSING_FAILED"] = "processedErrorCount"
    };

    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly AwsResourceOptions _options;

    public DynamoDbDailyMetricsRepository(IAmazonDynamoDB dynamoDb, IOptions<AwsResourceOptions> options)
    {
        _dynamoDb = dynamoDb;
        _options = options.Value;
    }

    public async Task IncrementAsync(string day, string eventType, DateTime updatedAtUtc, CancellationToken cancellationToken)
    {
        if (!CounterAttributeByEventType.TryGetValue(eventType, out var counterAttribute))
        {
            throw new InvalidOperationException($"Tipo de evento de metrica invalido: {eventType}");
        }

        await _dynamoDb.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _options.DailyMetricsTableName,
            Key = BuildKey(day),
            UpdateExpression = "SET #day = :day, #updatedAt = :updatedAt, #createdAt = if_not_exists(#createdAt, :createdAt) ADD #counter :increment",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#day"] = "day",
                ["#counter"] = counterAttribute,
                ["#createdAt"] = "createdAt",
                ["#updatedAt"] = "updatedAt"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":day"] = new AttributeValue { S = day },
                [":increment"] = new AttributeValue { N = "1" },
                [":createdAt"] = new AttributeValue { S = updatedAtUtc.ToString("O") },
                [":updatedAt"] = new AttributeValue { S = updatedAtUtc.ToString("O") }
            }
        }, cancellationToken);
    }

    public async Task<DailyMetricsRecord?> GetByDayAsync(string day, CancellationToken cancellationToken)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _options.DailyMetricsTableName,
            Key = BuildKey(day),
            ConsistentRead = true
        }, cancellationToken);

        if (response.Item is not { Count: > 0 })
        {
            return null;
        }

        return MapItem(day, response.Item);
    }

    private static DailyMetricsRecord MapItem(string day, Dictionary<string, AttributeValue> item)
    {
        return new DailyMetricsRecord
        {
            Day = day,
            ReceivedCount = ReadInt(item, "receivedCount"),
            ClassifiedCount = ReadInt(item, "classifiedCount"),
            ClassificationFailedCount = ReadInt(item, "classificationFailedCount"),
            ProcessedSuccessCount = ReadInt(item, "processedSuccessCount"),
            ProcessedErrorCount = ReadInt(item, "processedErrorCount"),
            CreatedAtUtc = ReadDate(item, "createdAt"),
            UpdatedAtUtc = ReadDate(item, "updatedAt")
        };
    }

    private static int ReadInt(Dictionary<string, AttributeValue> item, string key)
    {
        return item.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.N)
            ? int.Parse(value.N, System.Globalization.CultureInfo.InvariantCulture)
            : 0;
    }

    private static DateTime ReadDate(Dictionary<string, AttributeValue> item, string key)
    {
        return item.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.S)
            ? DateTime.Parse(value.S, null, System.Globalization.DateTimeStyles.RoundtripKind)
            : DateTime.MinValue;
    }

    private static Dictionary<string, AttributeValue> BuildKey(string day)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"DAY#{day}" },
            ["SK"] = new AttributeValue { S = MetadataSk }
        };
    }
}
