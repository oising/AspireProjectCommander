using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

internal sealed class ProjectCommanderHubLifecycleHook(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IDistributedApplicationEventing eventing,
    IServiceProvider services) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var hubResource = appModel.Resources.OfType<ProjectCommanderHubResource>().Single();

        var logger = loggerService.GetLogger(hubResource);
        hubResource.SetLogger(logger);

        await notificationService.PublishUpdateAsync(hubResource, state => state with
        {
            State = KnownResourceStates.Starting,
            CreationTimeStamp = DateTime.Now
        });

        try
        {
            await eventing.PublishAsync(
                new BeforeResourceStartedEvent(hubResource, services), cancellationToken);

            await hubResource.StartHubAsync();

            var hubUrl = await hubResource.ConnectionStringExpression.GetValueAsync(cancellationToken);

            await notificationService.PublishUpdateAsync(hubResource, state => state with
            {
                State = KnownResourceStates.Running,
                StartTimeStamp = DateTime.Now,
                Properties = [.. state.Properties, new("HubUrl", hubUrl)]
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Project Commands Hub: {Message}", ex.Message);

            await notificationService.PublishUpdateAsync(hubResource, state => state with
            {
                State = KnownResourceStates.FailedToStart
            });
        }
    }
}