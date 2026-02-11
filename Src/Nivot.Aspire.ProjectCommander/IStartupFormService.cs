namespace CommunityToolkit.Aspire.ProjectCommander;

/// <summary>
/// Service for managing startup form state and operations.
/// </summary>
public interface IStartupFormService
{
    /// <summary>
    /// Gets whether a startup form is required for this project.
    /// </summary>
    bool IsStartupFormRequired { get; }

    /// <summary>
    /// Gets whether the startup form has been completed.
    /// </summary>
    bool IsStartupFormCompleted { get; }

    /// <summary>
    /// Gets the startup form data if completed.
    /// </summary>
    Dictionary<string, string?>? StartupFormData { get; }

    /// <summary>
    /// Sets whether a startup form is required.
    /// </summary>
    void SetStartupFormRequired(bool required);

    /// <summary>
    /// Completes the startup form with the provided data.
    /// </summary>
    void CompleteStartupForm(Dictionary<string, string?> formData);

    /// <summary>
    /// Waits for the startup form to be completed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The form data dictionary, or null if no startup form is required.</returns>
    Task<Dictionary<string, string?>?> WaitForStartupFormAsync(CancellationToken cancellationToken = default);
}
