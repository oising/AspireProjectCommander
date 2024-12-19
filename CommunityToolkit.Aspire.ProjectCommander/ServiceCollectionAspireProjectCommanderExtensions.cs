using CommunityToolkit.Aspire.ProjectCommander;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionAspireProjectCommanderExtensions
{
    public static IServiceCollection AddAspireProjectCommanderClient(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        
        if (sp.GetService<IAspireProjectCommanderClient>() != null)
        {
            var worker = ActivatorUtilities.CreateInstance<AspireProjectCommanderClientWorker>(sp);
            services.AddSingleton<IHostedService>(worker);
            services.AddSingleton<IAspireProjectCommanderClient>(worker);
        }

        return services;
    }
}