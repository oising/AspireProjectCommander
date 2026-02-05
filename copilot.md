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
- Uses .NET SDK 9.0.100 (global.json).
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
