using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Represents the Aspire Project Commander SignalR Hub implementation.
/// </summary>
/// <param name="logger"></param>
/// <param name="loggerService"></param>
/// <param name="model"></param>
internal sealed class ProjectCommanderHub(ILogger logger, ResourceLoggerService loggerService, DistributedApplicationModel model) : Hub
{
    /// <summary>
    /// Identifies the connecting client by adding it to a group named after the resource.
    /// Also checks if the resource has a startup form and notifies the client.
    /// </summary>
    /// <param name="resourceName">The resource name (e.g., "datagenerator-abc123").</param>
    /// <returns></returns>
    [UsedImplicitly]
    public async Task Identify([ResourceName] string resourceName) //, ProjectCommand[]? commands = null)
    {
        logger.LogInformation("{ResourceName} connected to Aspire Project Commander Hub", resourceName);

        await Groups.AddToGroupAsync(Context.ConnectionId, resourceName);

        // Check if this resource has a startup form and notify the client
        var baseResourceName = resourceName.Split('-')[0];
        var resource = model.Resources.FirstOrDefault(r => r.Name == baseResourceName);

        if (resource != null)
        {
            var startupFormAnnotation = resource.Annotations.OfType<StartupFormAnnotation>().FirstOrDefault();
            if (startupFormAnnotation != null && !startupFormAnnotation.IsCompleted)
            {
                // Notify client that a startup form is required
                await Clients.Caller.SendAsync("StartupFormRequired", startupFormAnnotation.Form.Title);
                logger.LogInformation("{ResourceName} requires startup form: {Title}", resourceName, startupFormAnnotation.Form.Title);
            }
        }
    }

    /// <summary>
    /// Called by the project to signal that it has received and validated startup form data.
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="success">Whether the form was validated successfully.</param>
    /// <param name="errorMessage">Optional error message if validation failed.</param>
    [UsedImplicitly]
    public async Task StartupFormCompleted([ResourceName] string resourceName, bool success, string? errorMessage = null)
    {
        logger.LogInformation("{ResourceName} startup form completed: Success={Success}", resourceName, success);

        // Find the resource and update the annotation
        var baseResourceName = resourceName.Split('-')[0];
        var resource = model.Resources.FirstOrDefault(r => r.Name == baseResourceName);

        if (resource != null)
        {
            var annotation = resource.Annotations.OfType<StartupFormAnnotation>().FirstOrDefault();
            if (annotation != null)
            {
                annotation.IsCompleted = success;
                annotation.ErrorMessage = success ? null : errorMessage;
            }
        }

        // Notify dashboard/orchestrator that startup is complete
        await Clients.All.SendAsync("StartupFormStatusChanged", resourceName, success, errorMessage);
    }

    /// <summary>
    /// Allows remote clients to watch logs for a specific resource.
    /// </summary>
    /// <param name="resourceName"></param>
    /// <param name="take"></param>
    /// <returns></returns>
    [UsedImplicitly]
    public async IAsyncEnumerable<IReadOnlyList<LogLine>> WatchResourceLogs([ResourceName] string resourceName, int? take = null)
    {
        logger.LogTrace("Getting {LinesWanted} logs for resource {ResourceName}", take?.ToString() ?? "all", resourceName);

        int taken = 0;

        // resolve IResource from resource name
        var resource = model.Resources.SingleOrDefault(r => r.Name == resourceName);

        if (resource is null)
        {
            logger.LogWarning("Resource {ResourceName} not found", resourceName);
            yield break; // No matching resource found, exit
        }

        await foreach (var logs in loggerService.WatchAsync(resource)
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