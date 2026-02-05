using Aspire.Hosting.ApplicationModel;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Annotation that stores startup form configuration for a project resource.
/// When present, the project will be held in a "Waiting for Configuration" state
/// until the startup form is completed by the developer.
/// </summary>
public sealed class StartupFormAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Creates a new StartupFormAnnotation with the specified form definition.
    /// </summary>
    /// <param name="form">The startup form definition from the manifest.</param>
    public StartupFormAnnotation(StartupFormDefinition form)
    {
        Form = form ?? throw new ArgumentNullException(nameof(form));
    }

    /// <summary>
    /// The startup form definition.
    /// </summary>
    public StartupFormDefinition Form { get; }

    /// <summary>
    /// Whether the startup form has been completed by the user.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The form data submitted by the user, keyed by input name.
    /// </summary>
    public Dictionary<string, string?> FormData { get; set; } = new();

    /// <summary>
    /// Optional error message if the form submission failed validation.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
