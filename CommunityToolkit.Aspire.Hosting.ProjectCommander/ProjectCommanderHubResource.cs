using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

public sealed class ProjectCommanderHubResource([ResourceName] string name, ProjectCommanderHubOptions options)
    : Resource(name), IResourceWithConnectionString, IAsyncDisposable
{
    private WebApplication? _web;
    private ILogger? _logger;

    internal async Task StartHubAsync()
    {
        Hub = BuildHub();

        await (_web!.StartAsync()).ConfigureAwait(false);

        _logger?.LogInformation("Aspire Project Commander Hub started");
    }

    internal void SetLogger(ILogger logger) => _logger = logger;

    internal IHubContext<ProjectCommanderHub>? Hub { get; set; }

    private IHubContext<ProjectCommanderHub> BuildHub()
    {
        // we need the logger to be set before building the hub so we can inject it
        Debug.Assert(_logger != null, "Logger must be set before building hub");

        _logger?.LogInformation("Building SignalR Hub");

        // signalr project command host setup
        var host = WebApplication.CreateBuilder();

        // proxy logging to AppHost logger
        host.Services.AddSingleton(_logger!);

        host.WebHost.UseUrls($"{(options.UseHttps ? "https" : "http")}://localhost:{options.HubPort}");

        host.Services.AddSignalR();

        _web = host.Build();
        _web.UseRouting();
        _web.MapGet("/", () => "Aspire Project Commander Host 1.0, powered by SignalR.");
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

    public async ValueTask DisposeAsync()
    {
        if (_web != null) await _web.DisposeAsync();
    }
}