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