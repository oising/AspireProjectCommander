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
    /// If no manifest file exists, no commands are registered.
    /// This method can be combined with <see cref="WithProjectCommands{T}"/> to add additional commands.
    /// </summary>
    /// <typeparam name="T">The type of project resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithProjectManifest<T>(this IResourceBuilder<T> builder)
        where T : ProjectResource
    {
        var projectPath = GetProjectDirectory(builder.Resource);

        var manifest = ManifestReader.ReadManifest(projectPath);

        if (manifest == null)
        {
            // No manifest found, nothing to register
            return builder;
        }

        // Store startup form in annotation if present and register the configure command
        if (manifest.StartupForm != null)
        {
            var startupFormAnnotation = new StartupFormAnnotation(manifest.StartupForm);
            builder.WithAnnotation(startupFormAnnotation);

            // Add environment variable so the client knows it needs to wait for startup form
            builder.WithEnvironment("PROJECTCOMMANDER_STARTUP_FORM_REQUIRED", "true");

            // Add a "Configure" command to trigger the startup form prompt
            RegisterStartupFormCommand(builder, startupFormAnnotation);
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
            return builder.WithProjectCommands(projectCommands);
        }

        return builder;
    }

    /// <summary>
    /// Registers the startup form command for a resource with a startup form.
    /// The command is disabled after successful submission and requires a resource restart to re-enable.
    /// </summary>
    private static void RegisterStartupFormCommand<T>(IResourceBuilder<T> builder, StartupFormAnnotation annotation)
        where T : ProjectResource
    {
        var form = annotation.Form;
        var inputs = form.Inputs.Select(ManifestReader.ToInteractionInput).ToArray();

        builder.WithCommand(
            name: "projectcommander-configure",
            displayName: form.Title,
            executeCommand: async (context) =>
            {
                // Check if the startup form has already been completed
                if (annotation.IsCompleted)
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
                            Success = true,
                            ErrorMessage = "Configuration cancelled."
                        };
                    }

                    // Build form data dictionary
                    var formData = new Dictionary<string, string?>();
                    for (var i = 0; i < inputs.Length; i++)
                    {
                        formData[inputs[i].Name] = result.Data[i].Value;
                    }

                    // Send form data to the project
                    var groupName = context.ResourceName;
                    await hubResource.Hub.Clients.Group(groupName).SendAsync(
                        "ReceiveStartupForm",
                        formData,
                        context.CancellationToken);

                    // Update annotation to mark as completed
                    annotation.FormData = formData;
                    annotation.IsCompleted = true;

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
                UpdateState = (context) => annotation.IsCompleted
                    ? ResourceCommandState.Disabled
                    : ResourceCommandState.Enabled
            });
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
                // Thank you LLMs for the idea, but
                // // DO NOT DO THIS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // // it might work in simple cases, but it will fail in complex ones
                // // trust me, I've tried
                // // and this is what LLMs are good for, telling you what NOT to do
                // // because you are all wondering what the heck I'm talking about
                // // lets run this and see what happens
                // // because I know you are all thinking it
                // // and I don't want to hear it
                // i++;
                // builder.WithCommand($"dynamic-command-{i}", command.DisplayName, async (context) =>
                // {
                //     return new ExecuteCommandResult() { Success = true };
                // },
                // new CommandOptions { });

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
                            return new ExecuteCommandResult() { Success = true, ErrorMessage = "User cancelled command." };
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