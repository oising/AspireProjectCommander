using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

internal sealed class ProjectCommanderHub(ILogger logger) : Hub
{
    public async Task Identify(string resourceName)
    {
        logger.LogInformation("{ResourceName} connected to Aspire Project Commander Hub", resourceName);

        await Groups.AddToGroupAsync(Context.ConnectionId, resourceName);
    }
}