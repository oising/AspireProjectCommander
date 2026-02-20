# Changelog

All notable changes to Aspire Project Commander will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Project-Defined Commands via JSON Manifest

Projects can now define their own commands using a `projectcommander.json` manifest file placed in the project root directory. This enables projects to be self-describing and portable, without requiring command definitions in the AppHost.

**New extension method:**
- `WithProjectManifest<T>()` - Reads commands and startup forms from the project's `projectcommander.json` file; returns `IResourceBuilder<T>` for chaining

**Manifest features:**
- Define commands with name, display name, description, and icon
- Specify interactive inputs for commands (Text, SecretText, Choice, Boolean, Number)
- Define startup forms that must be completed before the project starts

#### Startup Form Resource

Startup forms are now represented as first-class Aspire resources. This enables using Aspire's native `WaitFor` semantics to block projects until configuration is complete.

**New types:**
- `StartupFormResource` - Custom Aspire resource representing a startup form
- `StartupFormResourceAnnotation` - Links a project to its startup form resource

**New extension method:**
- `WithStartupFormBehavior()` - Configures the startup form resource with the "Configure" command

**How it works:**
1. Define a `startupForm` section in your `projectcommander.json`
2. Call `WithProjectManifest()` — the startup form resource is automatically created, wired up, and the project is configured to wait for it
3. The form resource appears in the dashboard with state `WaitingForConfiguration`
4. When the user submits the form, the resource transitions to `Running` and the project starts

**Example:**
```csharp
builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(commander)
    .WaitFor(commander)
    .WithProjectManifest();
```

**Client-side:**
- `WaitForStartupFormAsync()` still works but returns immediately with cached data since Aspire handles blocking
- `IsStartupFormRequired` / `IsStartupFormCompleted` - Query form state
- `StartupFormReceived` event - Fires when form data is received

#### Combining Manifest and Code Commands

You can now use both `WithProjectManifest()` and `WithProjectCommands()` together. Commands from both sources are merged, with code-based commands taking precedence for duplicate names.

### New Files

| File | Purpose |
|------|---------|
| `ProjectCommandManifest.cs` | Manifest types for deserializing `projectcommander.json` |
| `ManifestReader.cs` | JSON parser and InputDefinition to InteractionInput converter |
| `StartupFormResource.cs` | Custom Aspire resource for startup forms |
| `StartupFormResourceAnnotation.cs` | Links project to its startup form resource |

### Removed Files

| File | Reason |
|------|--------|
| `StartupFormAnnotation.cs` | Replaced by `StartupFormResource` and `StartupFormResourceAnnotation` |

### Modified Files

| File | Changes |
|------|---------|
| `ResourceBuilderProjectCommanderExtensions.cs` | `WithProjectManifest()` automatically wires up the startup form resource |
| `DistributedApplicationBuilderExtensions.cs` | Added `WithStartupFormBehavior()` extension |
| `ProjectCommanderHub.cs` | Uses `StartupFormResourceAnnotation`, sends cached form data on connect |
| `IAspireProjectCommanderClient.cs` | Startup form interface members |
| `AspireProjectCommanderClientWorker.cs` | Handles new `ReceiveStartupForm` message format |

### Example Manifest

```json
{
  "version": "1.0",
  "startupForm": {
    "title": "Configure Data Generator",
    "inputs": [
      { "name": "delay", "label": "Delay (seconds)", "inputType": "Number", "required": true }
    ]
  },
  "commands": [
    { "name": "slow", "displayName": "Go Slow" },
    { "name": "fast", "displayName": "Go Fast" }
  ]
}
```

### Migration Guide

**From code-based commands to manifest-based:**

Before:
```csharp
builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(commander)
    .WithProjectCommands(
        new("slow", "Go Slow"),
        new("fast", "Go Fast"));
```

After:
1. Create `projectcommander.json` in your project root
2. Update AppHost:
```csharp
builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(commander)
    .WithProjectManifest();
```

**Adding startup form support to existing projects:**

1. Add `startupForm` section to your manifest
2. Call `await commander.WaitForStartupFormAsync(stoppingToken)` before your main work
3. Use the returned dictionary to configure your service

---

## [1.1.0] - Previous Release

- Initial release with code-based command definitions
- Remote resource log viewing via SpiraLog
- SignalR-based communication between AppHost and projects
