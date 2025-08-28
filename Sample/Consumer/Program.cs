// See https://aka.ms/new-console-template for more information

using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureEventHubConsumerClient("client");

builder.Services.AddAspireProjectCommanderClient();

builder.Services.AddHostedService<ConsumerWorker>();

var app = builder.Build();

app.Run();

internal sealed class ConsumerWorker(EventHubConsumerClient client, ILogger<ConsumerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(ConsumerWorker)} started");

        await Task.Run(async () =>
        {
            await foreach (var datum in client.ReadEventsAsync(stoppingToken))
            {
                logger.LogInformation("Partition: {PartitionId}; Id: {MessageId}",
                    datum.Partition.PartitionId, datum.Data.MessageId);
            }
        }, stoppingToken);

        logger.LogInformation($"{nameof(ConsumerWorker)} stopped");
    }
}