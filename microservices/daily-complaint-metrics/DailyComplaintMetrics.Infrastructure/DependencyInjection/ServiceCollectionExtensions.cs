using Amazon.DynamoDBv2;
using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Infrastructure.Options;
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
        services.AddSingleton<IDailyMetricsRepository, DynamoDbDailyMetricsRepository>();

        return services;
    }
}
