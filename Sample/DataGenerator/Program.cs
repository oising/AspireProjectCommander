// See https://aka.ms/new-console-template for more information

using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using CommunityToolkit.Aspire.ProjectCommander;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAspireProjectCommanderClient();

builder.AddAzureEventHubProducerClient("hub");

builder.Services.AddHostedService<DataGeneratorWorker>();

var app = builder.Build();

await app.RunAsync();

internal sealed class DataGeneratorWorker(IAspireProjectCommanderClient aspire, EventHubProducerClient producer, ILogger<DataGeneratorWorker> logger) : BackgroundService
{
    private bool _isPaused;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var json = """
{
    "data": {
        "foo": "bar",
        "bar": "baz",
        "cack": "quack"
    }
}
""";
        logger.LogInformation("Data generator worker started");

        // Wait for startup form to be completed (if required)
        var startupConfig = await aspire.WaitForStartupFormAsync(stoppingToken);

        // Apply startup configuration
        var period = TimeSpan.FromSeconds(1);
        if (startupConfig != null)
        {
            if (startupConfig.TryGetValue("initialDelay", out var delayStr) && int.TryParse(delayStr, out var delay))
            {
                period = TimeSpan.FromSeconds(delay);
                logger.LogInformation("Initial delay set to {Delay} seconds from startup form", delay);
            }

            if (startupConfig.TryGetValue("mode", out var mode))
            {
                logger.LogInformation("Generation mode set to: {Mode}", mode);
                _isPaused = mode == "On Demand";
            }
        }

        await Task.Run(async () =>
        {
            aspire.CommandReceived += (commandName, args, sp) =>
            {
                switch (commandName)
                {
                    case "slow":
                        period = TimeSpan.FromSeconds(1);
                        logger.LogInformation("Slow command received with args {Args}", string.Join(", ", args));
                        break;
                    case "fast":
                        period = TimeSpan.FromMilliseconds(10);
                        logger.LogInformation("Fast command received with args {Args}", string.Join(", ", args));
                        break;
                    case "specify":
                        logger.LogInformation("Specify command received with args {Args}", string.Join(", ", args));
                        period = TimeSpan.FromSeconds(int.Parse(args[0]));
                        logger.LogInformation("Period was set to {Period}", period);
                        break;
                    case "pause":
                        _isPaused = true;
                        logger.LogInformation("Data generation paused");
                        break;
                    case "resume":
                        _isPaused = false;
                        logger.LogInformation("Data generation resumed");
                        break;
                    default:
                        logger.LogWarning("Unknown command received: {CommandName}", commandName);
                        break;
                }

                return Task.CompletedTask;
            };

            // start pumping data
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(period, stoppingToken);

                if (!_isPaused)
                {
                    await producer.SendAsync([
                        new EventData(
                            Encoding.UTF8.GetBytes(json))], stoppingToken);
                }
            }
        }, stoppingToken);

        logger.LogInformation("Data generator worker stopped");
    }
}