#pragma warning disable ASPIREINTERACTION001

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.AspNetCore.SignalR;
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

    /// <summary>
    /// Configures the startup form resource with the "Configure" command and lifecycle management.
    /// Call this method after <see cref="ResourceBuilderProjectCommanderExtensions.WithProjectManifest{T}"/>
    /// to wire up the form's command handler and state transitions.
    /// </summary>
    /// <param name="builder">The startup form resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    internal static IResourceBuilder<StartupFormResource> WithStartupFormBehavior(
        this IResourceBuilder<StartupFormResource> builder)
    {
        var formResource = builder.Resource;
        var form = formResource.Form;
        var inputs = form.Inputs.Select(ManifestReader.ToInteractionInput).ToArray();

        // Register the "Configure" command on the startup form resource
        builder.WithCommand(
            name: "projectcommander-configure",
            displayName: form.Title,
            executeCommand: async (context) =>
            {
                // Check if the startup form has already been completed
                if (formResource.IsCompleted)
                {
                    return new ExecuteCommandResult
                    {
                        Success = false,
                        ErrorMessage = "Startup form has already been submitted. Restart the resource to configure again."
                    };
                }

                try
                {
                    var model = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
                    var hubResource = model.Resources.OfType<ProjectCommanderHubResource>().SingleOrDefault();

                    if (hubResource?.Hub == null)
                    {
                        return new ExecuteCommandResult
                        {
                            Success = false,
                            ErrorMessage = "Project Commander hub is not running."
                        };
                    }

                    var interaction = context.ServiceProvider.GetRequiredService<IInteractionService>();

                    // Show the startup form prompt
                    var result = await interaction.PromptInputsAsync(
                        form.Title,
                        form.Description ?? "Please configure the following settings:",
                        inputs,
                        cancellationToken: context.CancellationToken);

                    if (result.Canceled)
                    {
                        return new ExecuteCommandResult
                        {
                            Success = false,
                            ErrorMessage = "Configuration cancelled."
                        };
                    }

                    // Build form data dictionary
                    var formData = new Dictionary<string, string?>();
                    for (var i = 0; i < inputs.Length; i++)
                    {
                        formData[inputs[i].Name] = result.Data[i].Value;
                    }

                    // Mark the form resource as completed
                    formResource.MarkCompleted(formData);

                    // Get the logger for diagnostic output
                    var loggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
                    var logger = loggerService.GetLogger(formResource);

                    logger.LogInformation("Starting lifecycle transition for startup form '{FormName}'", formResource.Name);

                    // Send form data to the project via SignalR
                    var groupName = formResource.ParentProject.Name;
                    
                    // Get all instances of the parent project (handles replicas)
                    // The SignalR group is named after the resource instance name (e.g., "datagenerator-abc123")
                    // We need to send to all instances that match the base resource name
                    await hubResource.Hub.Clients.All.SendAsync(
                        "ReceiveStartupForm",
                        groupName,
                        formData,
                        context.CancellationToken);

                    logger.LogInformation("Sent startup form data to project '{ProjectName}'", groupName);

                    // Get the eventing service from the runtime service provider
                    var eventing = context.ServiceProvider.GetRequiredService<IDistributedApplicationEventing>();
                    logger.LogInformation("Resolved IDistributedApplicationEventing from ServiceProvider");

                    // Publish BeforeResourceStartedEvent to signal we're about to start.
                    // This is required for Aspire to properly track the resource lifecycle.
                    await eventing.PublishAsync(
                        new BeforeResourceStartedEvent(formResource, context.ServiceProvider),
                        context.CancellationToken);
                    logger.LogInformation("Published BeforeResourceStartedEvent");

                    // Transition the startup form resource to Running state.
                    var notify = context.ServiceProvider.GetRequiredService<ResourceNotificationService>();
                    await notify.PublishUpdateAsync(formResource, state => state with
                    {
                        State = KnownResourceStates.Running,
                        StartTimeStamp = DateTime.Now,
                        Properties = [
                            .. state.Properties,
                            new("form.completedAt", DateTime.Now.ToString("O"))
                        ]
                    });
                    logger.LogInformation("Transitioned to Running state");

                    await Task.Delay(500, context.CancellationToken);

                    // Now transition to Finished to indicate this is a completed one-time task
                    await notify.PublishUpdateAsync(formResource, state => state with
                    {
                        State = KnownResourceStates.Finished
                    });
                    logger.LogInformation("Transitioned to Finished state - lifecycle complete");

                    return new ExecuteCommandResult { Success = true };
                }
                catch (Exception ex)
                {
                    return new ExecuteCommandResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            },
            new CommandOptions
            {
                IconName = "Settings",
                IconVariant = IconVariant.Regular,
                IsHighlighted = true,
                // Dynamically update command state based on whether form is completed
                UpdateState = (context) => formResource.IsCompleted
                    ? ResourceCommandState.Disabled
                    : ResourceCommandState.Enabled
            });

        return builder;
    }
}