using CommunityToolkit.Aspire.Hosting.ProjectCommander;

var builder = DistributedApplication.CreateBuilder(args);

var commander = builder.AddAspireProjectCommander();

var datahub = builder.AddAzureEventHubs("data")
    .RunAsEmulator()
    .AddHub("hub")
    .WithProperties(configure =>
    {
        configure.PartitionCount = 2;
    });

var client = datahub.AddConsumerGroup("client");

builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(datahub)
    .WithReference(commander)
    .WaitFor(commander)
    .WaitFor(datahub)
    .WithProjectCommands(
        new("slow", "Go Slow"),
        new("fast", "Go Fast"));

builder.AddProject<Projects.Consumer>("consumer")
    .WithReference(commander)
    .WithReference(client)
    .WaitFor(datahub);

builder.Build().Run();
