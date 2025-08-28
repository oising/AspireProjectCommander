using CommunityToolkit.Aspire.ProjectCommander;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring the Aspire Project Commander client.
/// </summary>
public static class ServiceCollectionAspireProjectCommanderExtensions
{
    /// <summary>
    /// Adds the Aspire Project Commander client to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="unknown"></param>
    /// <returns></returns>
    public static IServiceCollection AddAspireProjectCommanderClient(this IServiceCollection services, object? unknown = null)
    {
        var sp = services.BuildServiceProvider();
        
        if (sp.GetService<IAspireProjectCommanderClient>() == null)
        {
            var worker = ActivatorUtilities.CreateInstance<AspireProjectCommanderClientWorker>(sp);
            services.AddSingleton<IHostedService>(worker);
            services.AddSingleton<IAspireProjectCommanderClient>(worker);
        }

        return services;
    }
}