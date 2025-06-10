using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

internal sealed class ProjectCommanderHub(ILogger logger, ResourceLoggerService resourceLogger, DistributedApplicationModel appModel) : Hub
{
    public async Task Identify([ResourceName] string resourceName)
    {
        logger.LogInformation("{ResourceName} connected to Aspire Project Commander Hub", resourceName);

        await Groups.AddToGroupAsync(Context.ConnectionId, resourceName);
    }

    public async IAsyncEnumerable<IReadOnlyList<LogLine>> WatchResourceLogs([ResourceName] string resourceName, int? take = null)
    {
        logger.LogInformation("Getting {LinesWanted} logs for resource {ResourceName}", take?.ToString() ?? "all", resourceName);

        int taken = 0;

        // resolve IResource from resource name
        var resource = appModel.Resources.SingleOrDefault(r => r.Name == resourceName);
        if (resource is null)
        {
            logger.LogWarning("Resource {ResourceName} not found", resourceName);
            yield break; // No matching resource found, exit
        }

        await foreach (var logs in resourceLogger.WatchAsync(resource)
            .WithCancellation(Context.ConnectionAborted)
            .ConfigureAwait(false))
        {
            if (take is null)
            {
                // No limit, return all logs
                yield return logs.ToList();
            }
            else if (taken < take.Value)
            {
                // Calculate how many more logs we can take
                int remaining = take.Value - taken;
                
                if (logs.Count <= remaining)
                {
                    // Return entire batch
                    yield return logs.ToList();
                    taken += logs.Count;
                }
                else
                {
                    // Return partial batch to reach exactly 'take' logs
                    yield return logs.Take(remaining).ToList();
                    taken = take.Value;
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }
}