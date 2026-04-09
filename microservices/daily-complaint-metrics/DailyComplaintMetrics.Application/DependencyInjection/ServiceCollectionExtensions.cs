using ComplaintClassifier.Application.Contracts;
using ComplaintClassifier.Application.Handlers;
using ComplaintClassifier.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ComplaintClassifier.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();

        services.AddTransient<UpdateDailyMetricsHandler>();
        services.AddTransient<GetDailyMetricsHandler>();

        return services;
    }
}
