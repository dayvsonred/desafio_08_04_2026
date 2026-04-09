using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Domain.Entities;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Infrastructure.Repositories;

public sealed class DynamoDbCategoryRepository : ICategoryRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly AwsResourceOptions _options;

    public DynamoDbCategoryRepository(IAmazonDynamoDB dynamoDb, IOptions<AwsResourceOptions> options)
    {
        _dynamoDb = dynamoDb;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<CategoryDefinition>> GetAllAsync(CancellationToken cancellationToken)
    {
        var categories = new List<CategoryDefinition>();
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;

        do
        {
            var response = await _dynamoDb.ScanAsync(new ScanRequest
            {
                TableName = _options.CategoriesTableName,
                ExclusiveStartKey = lastEvaluatedKey,
                ProjectionExpression = "#name, #description, #keywords",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#name"] = "name",
                    ["#description"] = "description",
                    ["#keywords"] = "keywords"
                }
            }, cancellationToken);

            categories.AddRange(response.Items.Select(item => new CategoryDefinition
            {
                Name = item["name"].S,
                Description = item["description"].S,
                Keywords = item["keywords"].L.Select(keyword => keyword.S).ToList()
            }));

            lastEvaluatedKey = response.LastEvaluatedKey;
        } while (lastEvaluatedKey is { Count: > 0 });

        return categories;
    }
}
