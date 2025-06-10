using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.ProjectCommander;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

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
        this IResourceBuilder<T> builder, params (string Name, string DisplayName)[] commands)
        where T : ProjectResource
    {
        if (commands.Length == 0)
        {
            throw new ArgumentException("You must supply at least one command.");
        }
        
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
                    
                    var groupName = context.ResourceName;
                    await hub.Clients.Group(groupName).SendAsync("ReceiveCommand", command.Name, context.CancellationToken);

                    success = true;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }
                return new ExecuteCommandResult() { Success = success, ErrorMessage = errorMessage };
            }, new CommandOptions {
                IconName = "DesktopSignal",
                IconVariant = IconVariant.Regular
            });
        }

        return builder;
    }
}