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
    /// <returns>Returns the updated service collection.</returns>
    public static IServiceCollection AddAspireProjectCommanderClient(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        
        if (sp.GetService<IAspireProjectCommanderClient>() is null)
        {
            var worker = ActivatorUtilities.CreateInstance<AspireProjectCommanderClientWorker>(sp);
            services.AddSingleton<IHostedService>(worker);
            services.AddSingleton<IAspireProjectCommanderClient>(worker);
        }

        return services;
    }
}