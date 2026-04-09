using System.Globalization;
using ComplaintClassifier.Application.DependencyInjection;
using ComplaintClassifier.Application.Options;
using ComplaintClassifier.Infrastructure.DependencyInjection;
using ComplaintClassifier.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ReceiveComplaint.Function.Bootstrap;

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

        var classificationOptions = BuildClassificationOptions(configuration);
        var awsOptions = BuildAwsResourceOptions(configuration);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddJsonConsole());
        services.AddSingleton<IConfiguration>(configuration);

        services.AddApplicationLayer(classificationOptions);
        services.AddAwsInfrastructure(awsOptions);

        return services.BuildServiceProvider();
    }

    private static ClassificationOptions BuildClassificationOptions(IConfiguration configuration)
    {
        return new ClassificationOptions
        {
            MinimumWinningScore = ReadInt(configuration, "Classification:MinimumWinningScore", 2),
            MinimumScoreGap = ReadInt(configuration, "Classification:MinimumScoreGap", 1),
            LowConfidenceThreshold = ReadDouble(configuration, "Classification:LowConfidenceThreshold", 0.75),
            StrongCategoryRatio = ReadDouble(configuration, "Classification:StrongCategoryRatio", 0.8),
            MaxStrongCategoriesBeforeLlm = ReadInt(configuration, "Classification:MaxStrongCategoriesBeforeLlm", 2)
        };
    }

    private static AwsResourceOptions BuildAwsResourceOptions(IConfiguration configuration)
    {
        return new AwsResourceOptions
        {
            ComplaintsTableName = configuration["AwsResources:ComplaintsTableName"] ?? "complaints",
            CategoriesTableName = configuration["AwsResources:CategoriesTableName"] ?? "categories",
            ClassificationQueueUrl = configuration["AwsResources:ClassificationQueueUrl"] ?? string.Empty,
            ProcessingQueueUrl = configuration["AwsResources:ProcessingQueueUrl"] ?? string.Empty,
            MetricsQueueUrl = configuration["AwsResources:MetricsQueueUrl"] ?? string.Empty,
            MessagesBucketName = configuration["AwsResources:MessagesBucketName"] ?? "itau_desafio_2026",
            BedrockModelId = configuration["AwsResources:BedrockModelId"] ?? "anthropic.claude-3-haiku-20240307-v1:0"
        };
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
        => int.TryParse(configuration[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static double ReadDouble(IConfiguration configuration, string key, double fallback)
        => double.TryParse(configuration[key], NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : fallback;
}
