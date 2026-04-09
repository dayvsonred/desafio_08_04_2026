using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Infrastructure.Storage;

public sealed class S3ComplaintMessageStorage : IComplaintMessageStorage
{
    private const string ReceivedPrefix = "complaint_message_received";
    private const string ProcessedPrefix = "complaint_message_processed";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonS3 _s3;
    private readonly AwsResourceOptions _options;
    private readonly ILogger<S3ComplaintMessageStorage> _logger;

    public S3ComplaintMessageStorage(
        IAmazonS3 s3,
        IOptions<AwsResourceOptions> options,
        ILogger<S3ComplaintMessageStorage> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveReceivedMessageAsync(
        string complaintId,
        string correlationId,
        string message,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken)
    {
        var key = $"{BuildDailyPrefix(ReceivedPrefix, receivedAtUtc)}/{complaintId}.json";

        var payload = new
        {
            complaintId,
            correlationId,
            message,
            receivedAtUtc = receivedAtUtc.ToString("O")
        };

        await PutJsonAsync(key, payload, cancellationToken);

        _logger.LogInformation(
            "Received complaint message saved to S3. complaintId={ComplaintId} key={S3Key}",
            complaintId,
            key);

        return key;
    }

    public async Task<string> LoadReceivedMessageAsync(string messageReceivedS3Key, CancellationToken cancellationToken)
    {
        var response = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _options.MessagesBucketName,
            Key = messageReceivedS3Key
        }, cancellationToken);

        await using var stream = response.ResponseStream;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(cancellationToken);

        var payload = JsonSerializer.Deserialize<ReceivedComplaintPayload>(json, JsonSerializerOptions)
            ?? throw new InvalidOperationException($"Arquivo S3 invalido: {messageReceivedS3Key}");

        if (string.IsNullOrWhiteSpace(payload.Message))
        {
            throw new InvalidOperationException($"Mensagem nao encontrada no arquivo S3: {messageReceivedS3Key}");
        }

        return payload.Message;
    }

    public async Task<string> SaveProcessedMessageAsync(
        string complaintId,
        string correlationId,
        string message,
        ClassificationResult classification,
        string? messageId,
        DateTime processedAtUtc,
        CancellationToken cancellationToken)
    {
        var safeMessageId = string.IsNullOrWhiteSpace(messageId) ? "unknown" : messageId.Trim();
        var key = $"{BuildDailyPrefix(ProcessedPrefix, processedAtUtc)}/{complaintId}_{safeMessageId}.json";

        var payload = new
        {
            complaintId,
            correlationId,
            messageId = safeMessageId,
            processedAtUtc = processedAtUtc.ToString("O"),
            message,
            classification = new
            {
                primaryCategory = classification.PrimaryCategory,
                secondaryCategories = classification.SecondaryCategories,
                confidence = classification.Confidence,
                decisionSource = classification.DecisionSource.ToString(),
                justification = classification.Justification,
                scoreBreakdown = classification.ScoreBreakdown.Select(score => new
                {
                    category = score.Category,
                    score = score.Score,
                    matchedKeywords = score.MatchedKeywords
                })
            }
        };

        await PutJsonAsync(key, payload, cancellationToken);

        _logger.LogInformation(
            "Processed complaint message saved to S3. complaintId={ComplaintId} key={S3Key}",
            complaintId,
            key);

        return key;
    }

    private async Task PutJsonAsync(string key, object payload, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(payload, JsonSerializerOptions);

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.MessagesBucketName,
            Key = key,
            ContentType = "application/json",
            ContentBody = body
        }, cancellationToken);
    }

    private static string BuildDailyPrefix(string basePrefix, DateTime timestampUtc)
        => $"{basePrefix}/{timestampUtc:yyyyMMdd}";

    private sealed class ReceivedComplaintPayload
    {
        public string? Message { get; init; }
    }
}