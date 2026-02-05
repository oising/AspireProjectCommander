# Changelog

All notable changes to Aspire Project Commander will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Project-Defined Commands via JSON Manifest

Projects can now define their own commands using a `projectcommander.json` manifest file placed in the project root directory. This enables projects to be self-describing and portable, without requiring command definitions in the AppHost.

**New extension method:**
- `WithProjectManifest<T>()` - Reads commands and startup forms from the project's `projectcommander.json` file

**Manifest features:**
- Define commands with name, display name, description, and icon
- Specify interactive inputs for commands (Text, SecretText, Choice, Boolean, Number)
- Define startup forms that must be completed before the project starts

#### Startup Forms

Projects can now require configuration before starting their main work via interactive startup forms.

**New client interface members:**
- `WaitForStartupFormAsync(CancellationToken)` - Blocks until the startup form is completed by the user
- `IsStartupFormRequired` - Indicates if a startup form is configured for this project
- `IsStartupFormCompleted` - Indicates if the startup form has been submitted
- `StartupFormReceived` event - Fires when startup form data is received

**How it works:**
1. Define a `startupForm` section in your `projectcommander.json`
2. Call `await commander.WaitForStartupFormAsync()` in your project's startup
3. The project waits for the user to click the Configure command in the dashboard
4. Once submitted, the form data is returned and the project continues

#### Combining Manifest and Code Commands

You can now use both `WithProjectManifest()` and `WithProjectCommands()` together. Commands from both sources are merged, with code-based commands taking precedence for duplicate names.

### New Files

| File | Purpose |
|------|---------|
| `ProjectCommandManifest.cs` | Manifest types for deserializing `projectcommander.json` |
| `ManifestReader.cs` | JSON parser and InputDefinition to InteractionInput converter |
| `StartupFormAnnotation.cs` | Resource annotation for tracking startup form state |

### Modified Files

| File | Changes |
|------|---------|
| `ResourceBuilderProjectCommanderExtensions.cs` | Added `WithProjectManifest()` and startup form command registration |
| `ProjectCommanderHub.cs` | Added startup form lifecycle methods |
| `IAspireProjectCommanderClient.cs` | Added startup form interface members |
| `AspireProjectCommanderClientWorker.cs` | Implemented startup form handling |

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
