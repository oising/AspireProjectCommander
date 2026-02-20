#pragma warning disable ASPIREINTERACTION001

using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.ProjectCommander;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

/// <summary>
/// Represents a command associated with a project, including its name and display name.
/// </summary>
/// <param name="Name">The unique name of the command. This value is typically used as an identifier.</param>
/// <param name="DisplayName">The user-friendly name of the command, intended for display in UI or logs.</param>
/// <param name="Arguments">Optional arguments to pass to the command.</param>
public record ProjectCommand(string Name, string DisplayName, params InteractionInput[] Arguments);

/// <summary>
/// Extension methods for configuring the Aspire Project Commander.
/// </summary>
public static class ResourceBuilderProjectCommanderExtensions
{
    /// <summary>
    /// Registers project commands from a projectcommander.json manifest file located in the project directory.
    /// If the manifest defines a startup form, a <see cref="StartupFormResource"/> is created and automatically
    /// configured so the project waits for it to be completed before starting.
    /// </summary>
    /// <typeparam name="T">The type of project resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithProjectManifest<T>(
        this IResourceBuilder<T> builder)
        where T : ProjectResource
    {
        var projectPath = GetProjectDirectory(builder.Resource);
        var manifest = ManifestReader.ReadManifest(projectPath);

        if (manifest == null)
        {
            // No manifest found, nothing to register
            return builder;
        }

        // Create startup form resource if present
        if (manifest.StartupForm != null)
        {
            var startupFormResource = new StartupFormResource(
                $"{builder.Resource.Name}-config",
                manifest.StartupForm,
                builder.Resource);

            // Add annotation to link parent project to startup form resource
            builder.WithAnnotation(new StartupFormResourceAnnotation(startupFormResource));

            // Add environment variable so the client knows it needs to wait for startup form
            builder.WithEnvironment("PROJECTCOMMANDER_STARTUP_FORM_REQUIRED", "true");

            // Add the startup form resource to the application model
            var startupFormBuilder = builder.ApplicationBuilder.AddResource(startupFormResource)
                .WithInitialState(new CustomResourceSnapshot
                {
                    ResourceType = "StartupForm",
                    State = StartupFormResource.WaitingForConfigurationState,
                    Properties = [
                        new(CustomResourceKnownProperties.Source, $"Startup form for {builder.Resource.Name}"),
                        new("form.title", manifest.StartupForm.Title),
                        new("form.inputCount", manifest.StartupForm.Inputs.Count.ToString())
                    ]
                })
                .ExcludeFromManifest();

            // Automatically wire up the startup form behavior, wait dependency, and parent relationship
            startupFormBuilder.WithStartupFormBehavior();
            builder.WaitFor(startupFormBuilder);
            startupFormBuilder.WithParentRelationship(builder);
        }

        // Register commands from manifest
        if (manifest.Commands.Count > 0)
        {
            var projectCommands = manifest.Commands
                .Select(c => new ProjectCommand(
                    c.Name,
                    c.DisplayName,
                    c.Inputs.Select(ManifestReader.ToInteractionInput).ToArray()))
                .ToArray();

            // Use the existing WithProjectCommands method to register
            builder.WithProjectCommands(projectCommands);
        }

        return builder;
    }

    /// <summary>
    /// Gets the project directory from a ProjectResource by reading its annotations.
    /// </summary>
    private static string GetProjectDirectory(ProjectResource resource)
    {
        // ProjectResource has IProjectMetadata annotation that contains the project path
        var metadata = resource.Annotations.OfType<IProjectMetadata>().FirstOrDefault();

        if (metadata == null)
        {
            throw new InvalidOperationException(
                $"Resource '{resource.Name}' does not have project metadata. " +
                "Ensure WithProjectManifest is called on a ProjectResource.");
        }

        return Path.GetDirectoryName(metadata.ProjectPath)
            ?? throw new InvalidOperationException(
                $"Could not determine project directory from path: {metadata.ProjectPath}");
    }

    /// <summary>
    /// Adds project commands to a project resource.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="commands"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IResourceBuilder<T> WithProjectCommands<T>(
        this IResourceBuilder<T> builder, params ProjectCommand[] commands)
        where T : ProjectResource
    {
        if (commands.Length == 0)
        {
            throw new ArgumentException("You must supply at least one command.");
        }

        // Add command proxies to the dashboard
        foreach (var command in commands)
        {
            builder.WithCommand(command.Name, command.DisplayName, async (context) =>
            {

                bool success = false;
                string errorMessage = string.Empty;

                try
                {
                    var model = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
                    var hub = model.Resources.OfType<ProjectCommanderHubResource>().Single().Hub!;

                    if (command.Arguments.Length > 0)
                    {
                        var interaction = context.ServiceProvider.GetRequiredService<IInteractionService>();
                        var result = await interaction.PromptInputsAsync($"Arguments for {command.Name}", $"Arguments {command.Name}", command.Arguments, cancellationToken : context.CancellationToken);

                        if (result.Canceled)
                        {
                            return new ExecuteCommandResult() { Success = false, ErrorMessage = "User cancelled command." };
                        }

                        var args = new string?[command.Arguments.Length];
                        for (var i = 0; i < command.Arguments.Length; i++)
                        {
                            args[i] = result.Data[i].Value;
                        }

                        var groupName = context.ResourceName;
                        await hub.Clients.Group(groupName).SendAsync("ReceiveCommand", command.Name, args, context.CancellationToken);
                    }
                    else
                    {
                        var groupName = context.ResourceName;
                        await hub.Clients.Group(groupName).SendAsync("ReceiveCommand", command.Name, Array.Empty<string>(), context.CancellationToken);
                    }

                    success = true;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }
                return new ExecuteCommandResult() { Success = success, ErrorMessage = errorMessage };
            },
            new CommandOptions
            {
                IconName = "DesktopSignal",
                IconVariant = IconVariant.Regular
            });
        }

        return builder;
    }
}