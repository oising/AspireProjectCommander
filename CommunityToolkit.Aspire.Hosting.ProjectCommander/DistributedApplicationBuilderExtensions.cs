using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

public static class DistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<ProjectCommanderHubResource> AddAspireProjectCommander(this IDistributedApplicationBuilder builder)
    {       
        return AddAspireProjectCommander(builder, new ProjectCommanderHubOptions());
    }

    public static IResourceBuilder<ProjectCommanderHubResource> AddAspireProjectCommander(this IDistributedApplicationBuilder builder, ProjectCommanderHubOptions options)
    {
        if (builder.Resources.Any(r => r.Name == "project-commander"))
        {
            throw new InvalidOperationException("project-commander resource already exists in the application model");
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

        builder.Services.TryAddLifecycleHook<ProjectCommanderHubLifecycleHook>();
       
        var resource = new ProjectCommanderHubResource("project-commander", options);

        return builder.AddResource(resource)
            .WithInitialState(new()
            {
                ResourceType = "ProjectCommander",
                State = "Stopped",
                Properties = [
                    new(CustomResourceKnownProperties.Source, "Project Commander"),
                ]
            })
            .ExcludeFromManifest();
    }
}