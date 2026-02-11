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
    /// <param name="startupFormService"></param>
    /// <param name="logger"></param>
    internal sealed class AspireProjectCommanderClientWorker(
        IConfiguration configuration, 
        IServiceProvider serviceProvider, 
        IStartupFormService startupFormService,
        ILogger<AspireProjectCommanderClientWorker> logger)
    : BackgroundService, IAspireProjectCommanderClient
    {
        private readonly List<Func<string, string[], IServiceProvider, Task>> _commandHandlers = new();
        private readonly List<Func<Dictionary<string, string?>, IServiceProvider, Task<bool>>> _startupFormHandlers = new();

        private HubConnection? _hub;
        private string? _aspireResourceName;

        /// <inheritdoc />
        public bool IsStartupFormRequired => startupFormService.IsStartupFormRequired;

        /// <inheritdoc />
        public bool IsStartupFormCompleted => startupFormService.IsStartupFormCompleted;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () =>
            {
                // Check if startup form is required via environment variable
                var isRequired = Environment.GetEnvironmentVariable("PROJECTCOMMANDER_STARTUP_FORM_REQUIRED") == "true";
                startupFormService.SetStartupFormRequired(isRequired);

                var connectionString = configuration.GetConnectionString("project-commander");

                if (connectionString == null)
                {
                    throw new InvalidOperationException("Connection string 'project-commander' not found");
                }

                _hub = new HubConnectionBuilder()
                    .WithUrl(connectionString)
                    .WithAutomaticReconnect()
                    .Build();

                // Wire up command handler
                _hub.On<string, string[]>("ReceiveCommand", async (command, args) =>
                {
                    logger.LogDebug("Received command: {CommandName} {Args}", command, string.Join(", ", args));

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

                // Wire up startup form handler
                _hub.On<Dictionary<string, string?>>("ReceiveStartupForm", async (formData) =>
                {
                    logger.LogInformation("Received startup form data with {Count} fields", formData.Count);

                    bool success = true;
                    string? errorMessage = null;

                    // Invoke all registered handlers
                    foreach (var handler in _startupFormHandlers)
                    {
                        try
                        {
                            var handlerResult = await handler(formData, serviceProvider);
                            if (!handlerResult)
                            {
                                success = false;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error processing startup form");
                            success = false;
                            errorMessage = ex.Message;
                            break;
                        }
                    }

                    // Notify the hub of completion
                    try
                    {
                        await _hub.InvokeAsync("StartupFormCompleted", _aspireResourceName, success, errorMessage, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error notifying hub of startup form completion");
                    }

                    if (success)
                    {
                        startupFormService.CompleteStartupForm(formData);
                        logger.LogInformation("Startup form completed successfully");
                    }
                    else
                    {
                        logger.LogWarning("Startup form validation failed: {Error}", errorMessage);
                    }
                });

                // Wire up startup form required notification (from hub)
                _hub.On<string>("StartupFormRequired", (title) =>
                {
                    logger.LogInformation("Startup form required: {Title}", title);
                    startupFormService.SetStartupFormRequired(true);
                });

                await _hub.StartAsync(stoppingToken);

                logger.LogInformation("Connected to Aspire Project Commands Hub: Registering identity...");

                // Grab my suffix from OTEL env vars so the AppHost signalr hub can correctly isolate this client (i.e. there may be replicas)
                var aspireServiceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")!;
                var aspireResourceSuffix = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES")!.Split("=")[1];
                _aspireResourceName = $"{aspireServiceName}-{aspireResourceSuffix}";

                await _hub.InvokeAsync("Identify", _aspireResourceName, stoppingToken);

                // block until shutdown / stop
                await Task.Delay(Timeout.Infinite, stoppingToken);

            }, stoppingToken);
        }

        /// <inheritdoc />
        public event Func<string, string[], IServiceProvider, Task> CommandReceived
        {
            add => _commandHandlers.Add(value);
            remove => _commandHandlers.Remove(value);
        }

        /// <inheritdoc />
        public event Func<Dictionary<string, string?>, IServiceProvider, Task<bool>>? StartupFormReceived
        {
            add
            {
                if (value != null)
                    _startupFormHandlers.Add(value);
            }
            remove
            {
                if (value != null)
                    _startupFormHandlers.Remove(value);
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, string?>?> WaitForStartupFormAsync(CancellationToken cancellationToken = default)
        {
            return await startupFormService.WaitForStartupFormAsync(cancellationToken);
        }
    }
}
