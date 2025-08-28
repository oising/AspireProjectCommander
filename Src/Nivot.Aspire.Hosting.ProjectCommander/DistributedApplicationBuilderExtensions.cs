using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Extension methods for configuring the Aspire Project Commander resource.
/// </summary>
public static class DistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Aspire Project Commander resource to the application model.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IResourceBuilder<ProjectCommanderHubResource> AddAspireProjectCommander(this IDistributedApplicationBuilder builder)
    {       
        return AddAspireProjectCommander(builder, new ProjectCommanderHubOptions());
    }

    /// <summary>
    /// Adds the Aspire Project Commander resource to the application model.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static IResourceBuilder<ProjectCommanderHubResource> AddAspireProjectCommander(this IDistributedApplicationBuilder builder, ProjectCommanderHubOptions options)
    {
        if (builder.Resources.Any(r => r.Name == ProjectCommanderHubResource.ResourceName))
        {
            throw new InvalidOperationException("ProjectCommanderHubResource already exists in the application model");
        }

        if (options == null) throw new ArgumentNullException(nameof(options));

        // ensure options.HubPort is > 1024 and < 65535
        if (options.HubPort < 1024 || options.HubPort > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(options.HubPort), "HubPort must be > 1024 and < 65535");
        }

        if (string.IsNullOrWhiteSpace(options.HubPath) || options.HubPath.Length < 2)
        {
            throw new ArgumentException("HubPath must be a valid path", nameof(options.HubPath));
        }
        
        var resource = new ProjectCommanderHubResource(options);

        builder.Eventing.Subscribe<InitializeResourceEvent>(resource, async (e, ct) =>
        {
            var notify = e.Services.GetRequiredService<ResourceNotificationService>();
            await notify.PublishUpdateAsync(resource, state => state with
            {
                State = KnownResourceStates.Starting,
                CreationTimeStamp = DateTime.Now
            });

            var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);
            logger.LogInformation("Initializing Aspire Project Commander Resource");

            await builder.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(resource, e.Services), ct);

            var loggerFactory = e.Services.GetRequiredService<ResourceLoggerService>();
            var model = e.Services.GetRequiredService<DistributedApplicationModel>();

            await resource.StartHubAsync(loggerFactory, model).ConfigureAwait(false);

            var hubUrl = await resource.ConnectionStringExpression.GetValueAsync(ct);

            await notify.PublishUpdateAsync(resource, state => state with
            {
                State = KnownResourceStates.Running,
                StartTimeStamp = DateTime.Now,
                Properties = [.. state.Properties, new("hub.url", hubUrl)]
            });
        });

        return builder.AddResource(resource)
            .WithInitialState(new()
            {
                ResourceType = "ProjectCommander",
                State = "Stopped",
                Properties = [
                    new(
                        CustomResourceKnownProperties.Source,
                        "Project Commander Host"),
                ],
#if !DEBUG
                IsHidden = true
#endif
            })
            .ExcludeFromManifest();
    }
}