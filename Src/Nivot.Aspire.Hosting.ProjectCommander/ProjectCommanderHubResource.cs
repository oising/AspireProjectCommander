using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Represents the Aspire Project Commander SignalR Hub Aspire resource.
/// </summary>
/// <param name="options"></param>
public sealed class ProjectCommanderHubResource(ProjectCommanderHubOptions options)
    : Resource(ResourceName), IResourceWithConnectionString, IAsyncDisposable
{
    internal const string ResourceName = "project-commander";

    private WebApplication? _web;
    private ILogger? _logger;

    internal async Task StartHubAsync(ResourceLoggerService loggerService, DistributedApplicationModel model)
    {
        Hub = BuildHub(loggerService, model);

        await (_web!.StartAsync()).ConfigureAwait(false);

        _logger?.LogInformation("Aspire Project Commander Hub started");
    }

    internal IHubContext<ProjectCommanderHub>? Hub { get; set; }

    private IHubContext<ProjectCommanderHub> BuildHub(ResourceLoggerService loggerService, DistributedApplicationModel model)
    {
        // Get logger for this resource
        _logger = loggerService.GetLogger(this);

        _logger.LogInformation("Building SignalR Hub");

        // signalr project command host setup
        var host = WebApplication.CreateBuilder();

        // used for streaming logs to clients
        host.Services.AddSingleton(loggerService);

        // require to resolve IResource from resource names
        host.Services.AddSingleton(model);

        // proxy logging to AppHost logger
        host.Services.AddSingleton(_logger);

        host.WebHost.UseUrls($"{(options.UseHttps ? "https" : "http")}://localhost:{options.HubPort}");

        host.Services.AddSignalR()
            .AddJsonProtocol(json => json.PayloadSerializerOptions.IncludeFields = true);

        _web = host.Build();
        _web.UseRouting();
        _web.MapGet("/", () => "Aspire Project Commander Host, powered by SignalR.");
        _web.MapHub<ProjectCommanderHub>(options.HubPath!);

        var hub = _web.Services.GetRequiredService<IHubContext<ProjectCommanderHub>>();

        _logger?.LogInformation("SignalR Hub built");

        return hub;
    }

    /// <summary>
    /// Gets the connection string expression for the SignalR Hub endpoint
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"{(options.UseHttps ? "https" : "http")}://localhost:{options.HubPort.ToString()}/{options.HubPath!.TrimStart('/')}");

    /// <summary>
    /// Disposes hosted resources
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (_web != null) await _web.DisposeAsync();
    }
}