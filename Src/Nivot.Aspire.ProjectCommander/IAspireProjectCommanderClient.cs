namespace CommunityToolkit.Aspire.ProjectCommander;

/// <summary>
/// Represents a client proxy that can receive commands from an Aspire AppHost project.
/// </summary>
public interface IAspireProjectCommanderClient
{
    /// <summary>
    /// Occurs when a command is received. The name of the command is passed as an argument.
    /// </summary>
    public event Func<string, string[], IServiceProvider, Task> CommandReceived;
}

/// <summary>
/// Provides extension methods for registering commands with an Aspire project commander client.
/// </summary>
public static class AspireProjectCommanderClientExtensions
{
    /// <summary>
    /// Registers a command with the specified name.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="commandName"></param>
    /// <returns></returns>

    public static IAspireProjectCommanderClient RegisterProjectCommand(this IAspireProjectCommanderClient client, string commandName)
    {
        return client;
    }
}

