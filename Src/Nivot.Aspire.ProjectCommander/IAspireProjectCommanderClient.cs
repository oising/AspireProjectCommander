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

    /// <summary>
    /// Occurs when startup form data is received from the AppHost.
    /// The handler should return true if validation succeeds, false otherwise.
    /// </summary>
    public event Func<Dictionary<string, string?>, IServiceProvider, Task<bool>>? StartupFormReceived;

    /// <summary>
    /// Gets whether a startup form is required for this project.
    /// </summary>
    bool IsStartupFormRequired { get; }

    /// <summary>
    /// Gets whether the startup form has been completed.
    /// </summary>
    bool IsStartupFormCompleted { get; }

    /// <summary>
    /// Waits for the startup form to be completed by the user.
    /// Returns the form data once submitted, or null if no startup form is configured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The form data dictionary, or null if no startup form is required.</returns>
    Task<Dictionary<string, string?>?> WaitForStartupFormAsync(CancellationToken cancellationToken = default);
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

