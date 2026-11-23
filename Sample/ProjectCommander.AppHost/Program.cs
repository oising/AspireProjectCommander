#pragma warning disable ASPIREINTERACTION001

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

var datagenerator = builder.AddDotnetProject("datagenerator", @"..\DataGenerator\DataGenerator.csproj")
    .WithReference(datahub)
    .WithReference(commander)
    .WaitFor(commander)
    .WaitFor(datahub)
    .WithProjectManifest(); // Reads commands and startup form from projectcommander.json

builder.AddDotnetProject("consumer", @"..\Consumer\Consumer.csproj")
    .WithReference(commander)
    .WaitFor(commander)
    .WithReference(client)
    .WaitFor(datahub);

builder.Build().Run();
