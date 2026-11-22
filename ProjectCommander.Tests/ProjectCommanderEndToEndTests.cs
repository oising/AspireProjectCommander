using Aspire.Hosting;
using CommunityToolkit.Aspire.Hosting.ProjectCommander;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace ProjectCommander.Tests;

public sealed class ProjectCommanderEndToEndTests
{
    [Fact]
    public async Task StartupFormAndCommandsReachRunningProjects()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var cancellationToken = timeout.Token;

        var interactionService = new Mock<IInteractionService>();
        interactionService
            .Setup(service => service.PromptInputsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<InteractionInput>>(),
                It.IsAny<InputsDialogInteractionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(InteractionResult.Ok(new InteractionInputCollection(
            [
                new InteractionInput { Name = "initialDelay", InputType = InputType.Number, Value = "1" },
                new InteractionInput { Name = "mode", InputType = InputType.Choice, Value = "Continuous" }
            ])));

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ProjectCommander_AppHost>([], cancellationToken);
        builder.Services.RemoveAll<IInteractionService>();
        builder.Services.AddSingleton(interactionService.Object);

        await using var app = await builder.BuildAsync(cancellationToken);
        var applicationStart = app.StartAsync(cancellationToken);

        var notifications = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceAsync("consumer", KnownResourceStates.Running, cancellationToken);
        await notifications.WaitForResourceAsync(
            "datagenerator-config",
            StartupFormResource.WaitingForConfigurationState,
            cancellationToken);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var loggerService = app.Services.GetRequiredService<ResourceLoggerService>();
        var consumer = model.Resources.Single(resource => resource.Name == "consumer");
        var dataGenerator = model.Resources.Single(resource => resource.Name == "datagenerator");
        var startupForm = model.Resources.OfType<StartupFormResource>().Single();

        var consumerEventLog = WaitForLogAsync(loggerService, consumer, "Partition:", cancellationToken);
        var startupFormLog = WaitForLogAsync(loggerService, dataGenerator, "Startup form completed with 2 fields", cancellationToken);
        var startupFormCommand = await app.Services.GetRequiredService<ResourceCommandService>()
            .ExecuteCommandAsync(startupForm, "projectcommander-configure", cancellationToken);

        Assert.True(startupFormCommand.Success, startupFormCommand.Message);
        await startupFormLog;
        await applicationStart;
        await notifications.WaitForResourceAsync("datagenerator", KnownResourceStates.Running, cancellationToken);
        await consumerEventLog;

        var pauseLog = WaitForLogAsync(loggerService, dataGenerator, "Data generation paused", cancellationToken);
        var pauseCommand = await app.Services.GetRequiredService<ResourceCommandService>()
            .ExecuteCommandAsync(dataGenerator, "pause", cancellationToken);

        Assert.True(pauseCommand.Success, pauseCommand.Message);
        await pauseLog;

        var resumeLog = WaitForLogAsync(loggerService, dataGenerator, "Data generation resumed", cancellationToken);
        var resumeCommand = await app.Services.GetRequiredService<ResourceCommandService>()
            .ExecuteCommandAsync(dataGenerator, "resume", cancellationToken);

        Assert.True(resumeCommand.Success, resumeCommand.Message);
        await resumeLog;
    }

    private static async Task WaitForLogAsync(
        ResourceLoggerService loggerService,
        IResource resource,
        string expectedText,
        CancellationToken cancellationToken)
    {
        await foreach (var logBatch in loggerService.WatchAsync(resource).WithCancellation(cancellationToken))
        {
            if (logBatch.Any(log => log.Content.Contains(expectedText, StringComparison.Ordinal)))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Resource '{resource.Name}' stopped before logging '{expectedText}'.");
    }
}
