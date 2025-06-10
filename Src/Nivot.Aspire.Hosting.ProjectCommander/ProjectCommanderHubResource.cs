using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// 
/// </summary>
/// <param name="name"></param>
/// <param name="options"></param>
public sealed class ProjectCommanderHubResource([ResourceName] string name, ProjectCommanderHubOptions options)
    : Resource(name), IResourceWithConnectionString, IAsyncDisposable
{
    private WebApplication? _web;
    private ILogger? _logger;
    private ResourceLoggerService? _resourceLogger;
    private DistributedApplicationModel? _appModel;

    internal async Task StartHubAsync()
    {
        Hub = BuildHub();

        await (_web!.StartAsync()).ConfigureAwait(false);

        _logger?.LogInformation("Aspire Project Commander Hub started");
    }

    internal void SetLogger(ResourceLoggerService logger) => _resourceLogger = logger;

    internal void SetModel(DistributedApplicationModel appModel) => _appModel = appModel;

    internal IHubContext<ProjectCommanderHub>? Hub { get; set; }

    private IHubContext<ProjectCommanderHub> BuildHub()
    {
        // we need the logger to be set before building the hub so we can inject it
        Debug.Assert(_resourceLogger != null, "ResourceLoggerService must be set before building hub");
        _logger = _resourceLogger.GetLogger(this);
        
        Debug.Assert(_appModel != null, "DistributedApplicationModel must be set before building hub");

        _logger.LogInformation("Building SignalR Hub");

        // signalr project command host setup
        var host = WebApplication.CreateBuilder();

        // used for streaming logs to clients
        host.Services.AddSingleton(_resourceLogger);

        // require to resolve IResource from resource names
        host.Services.AddSingleton(_appModel);

        // proxy logging to AppHost logger
        host.Services.AddSingleton(_logger);

        host.WebHost.UseUrls($"{(options.UseHttps ? "https" : "http")}://localhost:{options.HubPort}");

        host.Services.AddSignalR()
            .AddJsonProtocol(json => json.PayloadSerializerOptions.IncludeFields = true);

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

    /// <summary>
    /// Disposes hosted resources
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (_web != null) await _web.DisposeAsync();
    }
}