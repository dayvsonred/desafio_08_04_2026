using ComplaintClassifier.Application.DependencyInjection;
using ComplaintClassifier.Infrastructure.DependencyInjection;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyComplaintMetrics.Function.Bootstrap;

public static class ServiceProviderFactory
{
    private static readonly Lazy<IServiceProvider> Provider = new(CreateProvider);

    public static IServiceProvider GetProvider() => Provider.Value;

    private static IServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var awsOptions = new AwsResourceOptions
        {
            DailyMetricsTableName = configuration["AwsResources:DailyMetricsTableName"] ?? "daily-metrics"
        };

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddJsonConsole());
        services.AddSingleton<IConfiguration>(configuration);

        services.AddApplicationLayer();
        services.AddAwsInfrastructure(awsOptions);

        return services.BuildServiceProvider();
    }
}
