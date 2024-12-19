using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.ProjectCommander;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

public static class ResourceBuilderProjectCommanderExtensions
{
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
            }, iconName: "DesktopSignal", iconVariant: IconVariant.Regular);
        }

        return builder;
    }
}