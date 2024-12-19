namespace CommunityToolkit.Aspire.ProjectCommander;

/// <summary>
/// Represents a client proxy that can receive commands from an Aspire AppHost project.
/// </summary>
public interface IAspireProjectCommanderClient
{
    /// <summary>
    /// Occurs when a command is received. The name of the command is passed as an argument.
    /// </summary>
    public event Func<string, IServiceProvider, Task> CommandReceived;
}