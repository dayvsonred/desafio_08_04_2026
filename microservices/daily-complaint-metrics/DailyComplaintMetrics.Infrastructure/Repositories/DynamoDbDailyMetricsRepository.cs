using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace ComplaintClassifier.Infrastructure.Repositories;

public sealed class DynamoDbDailyMetricsRepository : IDailyMetricsRepository
{
    private const string MetadataSk = "METADATA";
    private const string ProcessedEventType = "PROCESSED";
    private const string ReceivedEventType = "RECEIVED";
    private const string DayPrefix = "DAY#";
    private const string DayEventSkPrefix = "EVENT#";
    private const string LookupComplaintPrefix = "LOOKUP#COMPLAINT#";
    private const string LookupCorrelationPrefix = "LOOKUP#CORRELATION#";
    private const string LookupProcessedSkPrefix = "PROCESSED#";
    private const string EventTimestampFormat = "yyyyMMdd'T'HHmmssfffffff";

    private static readonly IReadOnlyDictionary<string, string> CounterAttributeByEventType = new Dictionary<string, string>
    {
        ["RECEIVED"] = "receivedCount",
        ["CLASSIFIED"] = "classifiedCount",
        ["CLASSIFICATION_FAILED"] = "classificationFailedCount",
        ["PROCESSED"] = "processedSuccessCount",
        ["PROCESSING_FAILED"] = "processedErrorCount"
    };
    private static readonly HashSet<string> IndexedDayEventTypes = new(StringComparer.Ordinal)
    {
        ReceivedEventType,
        ProcessedEventType
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

    public async Task IndexMessageEventAsync(
        string day,
        string eventType,
        string complaintId,
        string correlationId,
        DateTime eventCreatedAtUtc,
        CancellationToken cancellationToken)
    {
        var normalizedEventType = eventType.Trim().ToUpperInvariant();
        var trimmedComplaintId = complaintId.Trim();
        var trimmedCorrelationId = correlationId.Trim();
        var timestampToken = eventCreatedAtUtc.ToString(EventTimestampFormat, CultureInfo.InvariantCulture);
        var eventCreatedAt = eventCreatedAtUtc.ToString("O");

        if (IndexedDayEventTypes.Contains(normalizedEventType))
        {
            await PutIfAbsentAsync(
                BuildDayKey(day),
                $"{DayEventSkPrefix}{normalizedEventType}#{timestampToken}#{trimmedComplaintId}",
                new Dictionary<string, AttributeValue>
                {
                    ["day"] = new AttributeValue { S = day },
                    ["eventType"] = new AttributeValue { S = normalizedEventType },
                    ["complaintId"] = new AttributeValue { S = trimmedComplaintId },
                    ["correlationId"] = new AttributeValue { S = trimmedCorrelationId },
                    ["eventCreatedAt"] = new AttributeValue { S = eventCreatedAt }
                },
                cancellationToken);
        }

        if (!string.Equals(normalizedEventType, ProcessedEventType, StringComparison.Ordinal))
        {
            return;
        }

        await PutIfAbsentAsync(
            $"{LookupComplaintPrefix}{trimmedComplaintId}",
            $"{LookupProcessedSkPrefix}{timestampToken}#{trimmedCorrelationId}",
            new Dictionary<string, AttributeValue>
            {
                ["day"] = new AttributeValue { S = day },
                ["eventType"] = new AttributeValue { S = normalizedEventType },
                ["complaintId"] = new AttributeValue { S = trimmedComplaintId },
                ["correlationId"] = new AttributeValue { S = trimmedCorrelationId },
                ["eventCreatedAt"] = new AttributeValue { S = eventCreatedAt }
            },
            cancellationToken);

        await PutIfAbsentAsync(
            $"{LookupCorrelationPrefix}{trimmedCorrelationId}",
            $"{LookupProcessedSkPrefix}{timestampToken}#{trimmedComplaintId}",
            new Dictionary<string, AttributeValue>
            {
                ["day"] = new AttributeValue { S = day },
                ["eventType"] = new AttributeValue { S = normalizedEventType },
                ["complaintId"] = new AttributeValue { S = trimmedComplaintId },
                ["correlationId"] = new AttributeValue { S = trimmedCorrelationId },
                ["eventCreatedAt"] = new AttributeValue { S = eventCreatedAt }
            },
            cancellationToken);
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

    public async Task<IReadOnlyList<DailyMetricMessageReference>> GetMessageEventsByDayAsync(
        string day,
        string eventType,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedEventType = eventType.Trim().ToUpperInvariant();
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _options.DailyMetricsTableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = BuildDayKey(day) },
                [":skPrefix"] = new AttributeValue { S = $"{DayEventSkPrefix}{normalizedEventType}#" }
            },
            ScanIndexForward = false,
            Limit = Math.Clamp(limit, 1, 500)
        }, cancellationToken);

        return response.Items.Select(MapMessageReference).ToList();
    }

    public async Task<IReadOnlyList<DailyMetricMessageReference>> GetProcessedEventsByComplaintIdAsync(
        string complaintId,
        int limit,
        CancellationToken cancellationToken)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _options.DailyMetricsTableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = $"{LookupComplaintPrefix}{complaintId.Trim()}" },
                [":skPrefix"] = new AttributeValue { S = LookupProcessedSkPrefix }
            },
            ScanIndexForward = false,
            Limit = Math.Clamp(limit, 1, 500)
        }, cancellationToken);

        return response.Items.Select(MapMessageReference).ToList();
    }

    public async Task<IReadOnlyList<DailyMetricMessageReference>> GetProcessedEventsByCorrelationIdAsync(
        string correlationId,
        int limit,
        CancellationToken cancellationToken)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _options.DailyMetricsTableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = $"{LookupCorrelationPrefix}{correlationId.Trim()}" },
                [":skPrefix"] = new AttributeValue { S = LookupProcessedSkPrefix }
            },
            ScanIndexForward = false,
            Limit = Math.Clamp(limit, 1, 500)
        }, cancellationToken);

        return response.Items.Select(MapMessageReference).ToList();
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

    private async Task PutIfAbsentAsync(
        string partitionKey,
        string sortKey,
        IReadOnlyDictionary<string, AttributeValue> attributes,
        CancellationToken cancellationToken)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = partitionKey },
            ["SK"] = new AttributeValue { S = sortKey }
        };

        foreach (var attribute in attributes)
        {
            item[attribute.Key] = attribute.Value;
        }

        try
        {
            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = _options.DailyMetricsTableName,
                ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)",
                Item = item
            }, cancellationToken);
        }
        catch (ConditionalCheckFailedException)
        {
            // Duplicate event delivery is expected in SQS retries.
        }
    }

    private static DailyMetricMessageReference MapMessageReference(Dictionary<string, AttributeValue> item)
    {
        return new DailyMetricMessageReference
        {
            Day = ReadString(item, "day"),
            ComplaintId = ReadString(item, "complaintId"),
            CorrelationId = ReadString(item, "correlationId"),
            EventType = ReadString(item, "eventType"),
            EventCreatedAtUtc = ReadDate(item, "eventCreatedAt")
        };
    }

    private static string ReadString(Dictionary<string, AttributeValue> item, string key)
    {
        return item.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.S)
            ? value.S
            : string.Empty;
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
            ["PK"] = new AttributeValue { S = BuildDayKey(day) },
            ["SK"] = new AttributeValue { S = MetadataSk }
        };
    }

    private static string BuildDayKey(string day) => $"{DayPrefix}{day}";
}
