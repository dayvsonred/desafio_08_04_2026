using Amazon.BedrockRuntime;
using Amazon.DynamoDBv2;
using Amazon.SQS;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Infrastructure.Bedrock;
using ComplaintClassifier.Infrastructure.Options;
using ComplaintClassifier.Infrastructure.Queue;
using ComplaintClassifier.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAwsInfrastructure(this IServiceCollection services, AwsResourceOptions options)
    {
        services.AddSingleton<IOptions<AwsResourceOptions>>(Microsoft.Extensions.Options.Options.Create(options));

        services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient());
        services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient());
        services.AddSingleton<IAmazonBedrockRuntime>(_ => new AmazonBedrockRuntimeClient());

        services.AddSingleton<IComplaintRepository, DynamoDbComplaintRepository>();
        services.AddSingleton<ICategoryRepository, DynamoDbCategoryRepository>();
        services.AddSingleton<IQueuePublisher, SqsQueuePublisher>();
        services.AddSingleton<IBedrockClassifierClient, BedrockClassifierClient>();

        return services;
    }
}
