using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Application.Options;
using ComplaintClassifier.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ComplaintClassifier.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services, ClassificationOptions? options = null)
    {
        services.AddSingleton<IOptions<ClassificationOptions>>(Microsoft.Extensions.Options.Options.Create(options ?? new ClassificationOptions()));

        services.AddSingleton<ITextNormalizer, TextNormalizer>();
        services.AddSingleton<IRuleBasedClassifier, RuleBasedClassifier>();
        services.AddSingleton<IClassificationOrchestrator, ClassificationOrchestrator>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IComplaintIdGenerator, GuidComplaintIdGenerator>();

        services.AddTransient<ReceiveComplaintHandler>();
        services.AddTransient<ClassifyComplaintHandler>();
        services.AddTransient<ProcessClassifiedComplaintHandler>();

        return services;
    }
}
