using CommunityToolkit.Aspire.Hosting.ProjectCommander;

var builder = DistributedApplication.CreateBuilder(args);

var commander = builder.AddAspireProjectCommander();

var datahub = builder.AddAzureEventHubs("data")
    .RunAsEmulator()
    .AddHub("hub");

builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(datahub)
    .WithReference(commander)
    .WaitFor(commander)
    .WaitFor(datahub)
    .WithProjectCommands(
        new("slow", "Go Slow"),
        new("fast", "Go Fast"));

builder.AddProject<Projects.Consumer>("consumer")
    .WithReference(datahub)
    .WaitFor(datahub);

builder.Build().Run();
