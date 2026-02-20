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

// WithProjectManifest now returns a tuple with the project and optional startup form resource
var (datagenerator, datageneratorConfig) = builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(datahub)
    .WithReference(commander)
    .WaitFor(commander)
    .WaitFor(datahub)
    .WithProjectManifest(); // Reads commands and startup form from projectcommander.json

// If the project has a startup form, configure it and make the project wait for it
if (datageneratorConfig is not null)
{
    datageneratorConfig.WithStartupFormBehavior();
    datagenerator.WaitFor(datageneratorConfig);
}

builder.AddProject<Projects.Consumer>("consumer")
    .WithReference(commander)
    .WaitFor(commander)
    .WithReference(client)
    .WaitFor(datahub);

builder.Build().Run();
