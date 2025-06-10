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

        await Task.Run(async () =>
        {
            var period = TimeSpan.FromSeconds(1);

            aspire.CommandReceived += (commandName, sp) =>
            {
                switch (commandName)
                {
                    case "slow":
                        period = TimeSpan.FromSeconds(1);
                        logger.LogInformation("Slow command received");
                        break;
                    case "fast":
                        period = TimeSpan.FromMilliseconds(10);
                        logger.LogInformation("Fast command received");
                        break;
                    default:
                        throw new NotSupportedException(commandName);
                }

                return Task.CompletedTask;
            };

            // start pumping data
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(period, stoppingToken);

                await producer.SendAsync([
                    new EventData(
                        Encoding.UTF8.GetBytes(json))], stoppingToken);
            }
        }, stoppingToken);

        logger.LogInformation("Data generator worker stopped");
    }
}