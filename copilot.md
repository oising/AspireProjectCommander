# Copilot guidance for AspireProjectCommander

## Project summary
This repo contains .NET Aspire libraries that enable custom project commands from the Aspire dashboard. There are two NuGet packages plus a sample Aspire app host and sample projects that demonstrate usage.

## Key projects and locations
- Libraries (NuGet):
  - Hosting package: Src/Nivot.Aspire.Hosting.ProjectCommander
  - Integration package: Src/Nivot.Aspire.ProjectCommander
- Sample Aspire app:
  - AppHost: Sample/ProjectCommander.AppHost
  - Service defaults: Sample/ProjectCommander.ServiceDefaults
  - Sample projects: Sample/DataGenerator, Sample/Consumer, Sample/SpiraLog
- Tests: ProjectCommander.Tests

## Target framework and SDK
- Uses .NET SDK 10.0.100 (global.json).
- Solutions: ProjectCommander.sln (primary), Packages.sln (packaging focus).

## Build, test, and run
- Build solution: dotnet build ProjectCommander.sln
- Run tests: dotnet test ProjectCommander.Tests/ProjectCommander.Tests.csproj
- Run sample AppHost with the Aspire CLI: aspire run

## Conventions and tips for agents
- Prefer editing library code under Src/* for product changes; Sample/* is for demos.
- Avoid reformatting unrelated code; keep existing patterns and public APIs stable.
- When adding new public surface area, update README.md if it impacts usage examples.
- Packaging metadata is centralized in Src/Directory.Build.props.

## Common entry points
- Hosting extensions and resource wiring live in:
  - Src/Nivot.Aspire.Hosting.ProjectCommander/DistributedApplicationBuilderExtensions.cs
  - Src/Nivot.Aspire.Hosting.ProjectCommander/ResourceBuilderProjectCommanderExtensions.cs
- Integration client and DI entry points live in:
  - Src/Nivot.Aspire.ProjectCommander/ServiceCollectionAspireProjectCommanderExtensions.cs
  - Src/Nivot.Aspire.ProjectCommander/AspireProjectCommanderClientWorker.cs

## Tests
- Keep new tests alongside ProjectCommander.Tests.
- Favor integration-style tests when validating end-to-end command flow.

## Aspire custom resource lifecycle patterns

### WaitFor and ResourceReadyEvent
When creating custom Aspire resources that other resources can `WaitFor`:

1. **Subscribe to `InitializeResourceEvent`** - Custom resources must opt-in to Aspire's lifecycle by subscribing to this event. Without this, the resource isn't tracked by Aspire's orchestrator.

2. **Publish `BeforeResourceStartedEvent`** before transitioning to `Running` state - This signals to Aspire that the resource is about to start.

3. **Publish `ResourceReadyEvent` for process-less custom resources** - Aspire automatically publishes `ResourceReadyEvent` for built-in types (Container, Project, Executable) that have actual processes. For custom resources without a process (like `StartupFormResource`), you must manually publish this event to unblock `WaitFor` dependents.

4. **Resolve services from runtime `ServiceProvider`, not build-time builder** - When publishing events from command handlers or callbacks that execute at runtime:
   ```csharp
   // WRONG - captured at build time, may not work at runtime
   await builder.ApplicationBuilder.Eventing.PublishAsync(...);
   
   // CORRECT - resolved at runtime
   var eventing = context.ServiceProvider.GetRequiredService<IDistributedApplicationEventing>();
   await eventing.PublishAsync(...);
   ```

### State machine for one-time configuration resources
For resources like startup forms that block until user input:
1. Initial state: Custom state (e.g., `WaitingForConfiguration`)
2. After user completes form: `Running` → publish `ResourceReadyEvent` → `Finished`

### Key Aspire eventing types
- `InitializeResourceEvent` - First event fired for any resource
- `BeforeResourceStartedEvent` - Just before execution begins
- `ResourceReadyEvent` - Unblocks dependents waiting via `WaitFor`
- Namespace: `Aspire.Hosting.Eventing`

### Reference documentation
- Aspire app model spec: https://github.com/dotnet/aspire/blob/main/docs/specs/appmodel.md
