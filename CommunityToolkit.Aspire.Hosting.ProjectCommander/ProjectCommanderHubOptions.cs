namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Options for configuring the ProjectCommanderHub
/// </summary>
public class ProjectCommanderHubOptions
{
    public const int DefaultHubPort = 27960;
    public const string DefaultHubPath = "/projectcommander";

    /// <summary>
    /// Gets or sets the port the hub will listen on. Defaults to 27960.
    /// </summary>
    public int HubPort { get; set; } = DefaultHubPort;
    /// <summary>
    /// Gets or sets the path the hub will listen on. Defaults to "/projectcommander".
    /// </summary>
    public string? HubPath { get; set; } = DefaultHubPath;
    /// <summary>
    /// Gets or sets whether to use HTTPS for the hub. Defaults to true.
    /// </summary>
    public bool UseHttps { get; set; } = true;
}