[![publish](https://github.com/oising/AspireProjectCommander/actions/workflows/main.yml/badge.svg)](https://github.com/oising/AspireProjectCommander/actions/workflows/main.yml)

# Aspire Project Commander

![icon](https://github.com/user-attachments/assets/a087a57f-63fe-43f6-ad72-e774eef86236)

Aspire Project Commander is a set of packages that lets you send simple string commands from the dashboard directly to projects, and now supports **project-defined commands** via JSON manifests and **startup forms** for interactive project configuration.

## NuGet Packages

|Type|Name|Status|
|-|-|-|
|Integration|`Nivot.Aspire.ProjectCommander`|![NuGet Version](https://img.shields.io/nuget/v/Nivot.Aspire.ProjectCommander)|
|Hosting|`Nivot.Aspire.Hosting.ProjectCommander`|![NuGet Version](https://img.shields.io/nuget/v/Nivot.Aspire.Hosting.ProjectCommander)|

## Features

- **Custom Project Commands** - Send commands from the Aspire Dashboard to running projects
- **Project-Defined Commands (New!)** - Projects can define their own commands via a `projectcommander.json` manifest
- **Startup Forms (New!)** - Projects can require configuration before starting via interactive forms
- **Interactive Inputs** - Commands can prompt for user input (Text, Number, Choice, Boolean, SecretText)
- **Remote Log Viewing** - Stream resource logs to a terminal window

## Custom Resource Commands
[Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) allows adding [custom commands](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/custom-resource-commands) to any project in the dashboard but these commands are scoped to and handled in the AppHost itself. These are useful to send commands to APIs on running containers, such as performing a `FLUSHALL` on a Redis container to reset state. Ultimately, the `WithCommand` resource extension method requires you to interface with each target resource (e.g. `Executable`, `Container`, `Project`) independently, using code you write yourself.

## Custom Project Commands
This project and its associated NuGet packages allow you to send simple commands directly to `Project` type resources, that is to say, regular dotnet projects you're writing yourself. Register some simple string commands in the [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview?tabs=bash) -- for example "start-messages", "stop-messages" -- using the [hosting](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview?tabs=docker) package `Nivot.Aspire.Hosting.ProjectCommander`, and then use the [integration](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/integrations-overview) package `Nivot.Aspire.ProjectCommander` to receive commands in your message generating project that you're using to dump data into an Azure Event Hubs emulator.

## Project-Defined Commands (New!)

Instead of defining commands in the AppHost, projects can now define their own commands using a `projectcommander.json` manifest file. This allows projects to be self-describing and portable.

### Manifest File: `projectcommander.json`

Place this file in your project root (next to the `.csproj` file):

```json
{
  "$schema": "https://raw.githubusercontent.com/oising/AspireProjectCommander/main/schemas/projectcommander-v1.schema.json",
  "version": "1.0",
  "startupForm": {
    "title": "Configure Data Generator",
    "description": "Please configure the data generator settings before starting.",
    "inputs": [
      {
        "name": "initialDelay",
        "label": "Initial Delay (seconds)",
        "inputType": "Number",
        "required": true
      },
      {
        "name": "mode",
        "label": "Generation Mode",
        "inputType": "Choice",
        "required": true,
        "options": ["Continuous", "Burst", "On Demand"]
      }
    ]
  },
  "commands": [
    {
      "name": "slow",
      "displayName": "Go Slow",
      "iconName": "Clock"
    },
    {
      "name": "fast",
      "displayName": "Go Fast",
      "iconName": "FastForward"
    },
    {
      "name": "specify",
      "displayName": "Specify Delay...",
      "inputs": [
        {
          "name": "delay",
          "label": "Delay (seconds)",
          "inputType": "Number",
          "required": true
        }
      ]
    }
  ]
}
```

### Supported Input Types

| Type | Description |
|------|-------------|
| `Text` | Single-line text input |
| `SecretText` | Masked password-style input |
| `Choice` | Selection from predefined options |
| `Boolean` | True/false toggle |
| `Number` | Numeric value entry |

### Using the Manifest in AppHost

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var commander = builder.AddAspireProjectCommander();

// WithProjectManifest returns a tuple: (project builder, optional startup form resource)
var (datagenerator, datageneratorConfig) = builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(commander)
    .WaitFor(commander)
    .WithProjectManifest(); // Reads commands and startup form from projectcommander.json

// If the project has a startup form, configure it and make the project wait for it
if (datageneratorConfig is not null)
{
    datageneratorConfig.WithStartupFormBehavior();
    datagenerator.WaitFor(datageneratorConfig); // Project won't start until form is completed
}

builder.Build().Run();
```

The startup form appears as a separate resource in the Aspire dashboard with state `WaitingForConfiguration`. 
The project is blocked by Aspire's `WaitFor` until the developer clicks "Configure" and submits the form, 
at which point the form resource transitions to `Running` and the project starts.

### Handling Startup Forms in Projects

When using the `WaitFor(startupFormResource)` pattern, Aspire blocks the project from starting until the form is completed. 
Once the project starts, it can retrieve the form data using `WaitForStartupFormAsync()` which returns immediately with the cached values:

```csharp
public sealed class DataGeneratorWorker(
    IAspireProjectCommanderClient commander,
    ILogger<DataGeneratorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get startup form data (returns immediately since form was already completed before project started)
        var config = await commander.WaitForStartupFormAsync(stoppingToken);

        if (config != null)
        {
            var delay = int.Parse(config["initialDelay"] ?? "1");
            var mode = config["mode"];
            logger.LogInformation("Starting with delay={Delay}, mode={Mode}", delay, mode);
        }

        // Register command handlers
        commander.CommandReceived += (cmd, args, sp) =>
        {
            switch (cmd)
            {
                case "slow": /* handle */ break;
                case "fast": /* handle */ break;
                case "specify":
                    var seconds = int.Parse(args[0]);
                    break;
            }
            return Task.CompletedTask;
        };

        // Main work loop
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

## Remote Resource Log Viewing
Some people may prefer to stream resource logs in a terminal window. See the `SpiraLog` sample in the source.

## Code-Based Commands (Original Approach)

You can still define commands directly in the AppHost using `WithProjectCommands`:

### AppHost Hosting

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var commander = builder.AddAspireProjectCommander();

builder.AddProject<Projects.DataGenerator>("eventhub-datagenerator")
    // provides commander signalr hub connectionstring to integration
    .WithReference(commander)
    // array of simple tuples with the command string and a display value for the dashboard
    .WithProjectCommands(
        new("slow", "Go Slow"),
        new("fast", "Go Fast"),
        new("specify", "Specify Delay...",
            new InteractionInput { Name = "delay", Label = "period", InputType = InputType.Number }))
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
            commander.CommandReceived += (string command, string[] args, IServiceProvider sp) =>
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

## Combining Manifest and Code Commands

You can use both `WithProjectManifest()` and `WithProjectCommands()` together - the commands will be merged:

```csharp
builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(commander)
    .WithProjectManifest()                              // Commands from manifest
    .WithProjectCommands(new("extra", "Extra Command")); // Additional code-defined command
```

## Sample

See the `Sample` folder for an Aspire example that allows you to signal a data generator project that is writing messages into an emulator instance of Azure Event Hubs.
