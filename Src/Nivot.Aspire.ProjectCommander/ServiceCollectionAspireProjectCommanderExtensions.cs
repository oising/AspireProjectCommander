using CommunityToolkit.Aspire.ProjectCommander;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring the Aspire Project Commander client.
/// </summary>
public static class ServiceCollectionAspireProjectCommanderExtensions
{
    private static bool _isRegistered;

    /// <summary>
    /// Adds the Aspire Project Commander client to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <returns>Returns the updated service collection.</returns>
    public static IServiceCollection AddAspireProjectCommanderClient(this IServiceCollection services)
    {
        // No way to use the TryAdd* variants as they don't cover this scenario/overloads
        if (!_isRegistered)
        {
            services.AddSingleton<IStartupFormService, StartupFormService>();
            services.AddSingleton<AspireProjectCommanderClientWorker>();
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AspireProjectCommanderClientWorker>());
            services.AddSingleton<IAspireProjectCommanderClient>(sp => sp.GetRequiredService<AspireProjectCommanderClientWorker>());
            _isRegistered = true;
        }

        return services;
    }
}