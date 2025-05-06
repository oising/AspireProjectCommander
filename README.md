[![publish](https://github.com/oising/AspireProjectCommander/actions/workflows/main.yml/badge.svg)](https://github.com/oising/AspireProjectCommander/actions/workflows/main.yml)

# Aspire Project Commander

![icon](https://github.com/user-attachments/assets/a087a57f-63fe-43f6-ad72-e774eef86236)

Aspire Project commander is a set of packages that lets you send simple string commands from the dashboard directly to projects.

## NuGet Packages

|Type|Name|Status|
|-|-|-|
|Integration|`Nivot.Aspire.ProjectCommander`|![NuGet Version](https://img.shields.io/nuget/v/Nivot.Aspire.ProjectCommander)|
|Hosting|`Nivot.Aspire.Hosting.ProjectCommander`|![NuGet Version](https://img.shields.io/nuget/v/Nivot.Aspire.Hosting.ProjectCommander)|

## Custom Resource Commands
[Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) allows adding [custom commands](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/custom-resource-commands) to any project in the dashboard but these commands are scoped to and handled in the AppHost itself. These are useful to send commands to APIs on running containers, such as performing a `FLUSHALL` on a Redis container to reset state. Ultimately, the `WithCommand` resource extension method requires you to interface with each target resource (e.g. `Executable`, `Container`, `Project`) independently, using code you write yourself.

## Custom Project Commands (New!)
This project and its associated NuGet packages allow you to send simple commands directly to `Project` type resources, that is to say, regular dotnet projects you're writing yourself. Register some simple string commands in the [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview?tabs=bash) -- for example "start-messages", "stop-messages" -- using the [hosting](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview?tabs=docker) package `Nivot.Aspire.Hosting.ProjectCommander`, and then use the [integration](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/integrations-overview) package `Nivot.Aspire.ProjectCommander` to receive commands in your message generating project that you're using to dump data into an Azure Event Hubs emulator. 

## Example

See the `Sample` folder for an Aspire example that allows you to signal a data generator project that is writing messages into an emulator instance of Azure Event Hubs. 

### AppHost Hosting

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var commander = builder.AddAspireProjectCommander();

builder.AddProject<Projects.DataGenerator>("eventhub-datagenerator")
    // provides commander signalr hub connectionstring to integration 
    .WithReference(commander)
    // array of simple tuples with the command string and a display value for the dashbaord 
    .WithProjectCommands((Name: "slow", DisplayName: "Go Slow"), ("fast", "Go Fast"))
    // wait for commander signalr hub to be ready    
    .WaitFor(commander);

var app = builder.Build();

await app.RunAsync();
```
![image](https://github.com/user-attachments/assets/c1eb70e7-410e-49e6-92ba-db66ae7be563)

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
