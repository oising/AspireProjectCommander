using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.ProjectCommander
{
    /// <summary>
    /// Background service that connects to the Aspire Project Commander SignalR Hub and listens for commands.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="logger"></param>
    internal sealed class AspireProjectCommanderClientWorker(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<AspireProjectCommanderClientWorker> logger)
    : BackgroundService, IAspireProjectCommanderClient
    {
        private readonly List<Func<string, string[], IServiceProvider, Task>> _commandHandlers = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () =>
            {
                // TODO: maybe hardcode to a wellknown value, i.e. a unique guid?
                var connectionString = configuration.GetConnectionString("project-commander");
                
                if (connectionString == null)
                {
                    throw new InvalidOperationException("Connection string 'project-commander' not found");
                }

                var hub = new HubConnectionBuilder()
                    .WithUrl(connectionString)
                    .WithAutomaticReconnect()
                    .Build();

                // Wire up a command handler
                hub.On<string, string[]>("ReceiveCommand", async (command, args) =>
                {
                    logger.LogDebug("Received command: {CommandName} {Args}", command, string.Join(", ", args));

                    // note: could be optimized to run in parallel
                    foreach (var handler in _commandHandlers)
                    {
                        try
                        {
                            await handler(command, args, serviceProvider);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error invoking handler for command: {CommandName}", command);
                        }
                    }
                });

                await hub.StartAsync(stoppingToken);

                logger.LogInformation("Connected to Aspire Project Commands Hub: Registering identity...");

                // Grab my suffix from OTEL env vars so the AppHost signalr hub can correctly isolate this client (i.e. there may be replicas)
                var aspireResourceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")!;
                var aspireResourceSuffix = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES")!.Split("=")[1];
                
                await hub.InvokeAsync("Identify", $"{aspireResourceName}-{aspireResourceSuffix}", stoppingToken);

                // block until shutdown / stop
                await Task.Delay(Timeout.Infinite, stoppingToken);

            }, stoppingToken);
        }

        public event Func<string, string[], IServiceProvider, Task> CommandReceived
        {
            add => _commandHandlers.Add(value);
            remove => _commandHandlers.Remove(value);
        }
    }
}