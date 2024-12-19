# Aspire Project Commander

## Custom Resource Commands
[Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) allows adding [custom commands](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/custom-resource-commands) to any project in the dashboard but these commands are scoped to and handled in the AppHost itself. These are useful to send commands to APIs on running containers, such as performing a `FLUSHALL` on a Redis container to reset state. Ultimately, the `WithCommand` resource extension method requires you to interface with each target resource (e.g. `Executable`, `Container`, `Project`) independently, using code you write yourself.

## Custom Project Commands (New!)
This project and its associated NuGet packages allow you to send simple commands directly to `Project` type resources, that is to say, regular dotnet projects you're writing yourself. Register some simple string commands in the [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview?tabs=bash) -- for example "start-messages", "stop-messages" -- using the [hosting](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview?tabs=docker) package `CommunityToolkit.Aspire.Hosting.ProjectCommander`, and then use the [integration](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/integrations-overview) package `CommunityToolkit.Aspire.ProjectCommander` to receive commands in your message generating project that you're using to dump data into an Azure Event Hubs emulator. 

## Example

### AppHost Hosting

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var commander = builder.AddAspireProjectCommander();

builder.AddProject<Projects.DataGenerator>("eventhub-datagenerator")
    // provides commander signalr hub connectionstring to integration 
    .WithReference(commander)
    // array of simple tuples with the command string and a display value for the dashbaord 
    .WithProjectCommands((Name: "start", DisplayName: "Start Generator"), ("stop", "Stop Generator"))
    // wait for commander signalr hub to be ready    
    .WaitFor(commander);

var app = builder.Build();

await app.RunAsync();
```

### Project Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// add client to connect to apphost
builder.Services.AddAspireProjectCommanderClient();

// add background service to handle commands
builder.Services.AddHostedService<MyProjectCommands>();

// background service with DI IAspireProjectCommanderClient interface that allows registering an async handler
public sealed class MyProjectCommands(IAspireProjectCommanderClient commander, ILogger<MyProjectCommands> logger) : BackgroundService
{    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            // add a handler that will receive commands
            commander.CommandReceived += (string command, IServiceProvider sp) =>
            {
                // grab a service, call a method, set an option, signal a cancellation token etc...
                logger.LogInformation("Received command: {CommandName}", command);

                return Task.CompletedTask;
            };

            await Task.Delay(Timeout.Infinite, stoppingToken);

        }, stoppingToken);
    }
}
```
