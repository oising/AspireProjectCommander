using Aspire.Hosting.ApplicationModel;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Represents a startup form as an Aspire resource. Projects can use WaitFor
/// to wait for this resource to transition to Running state (form completed).
/// </summary>
public sealed class StartupFormResource : Resource
{
    /// <summary>
    /// Custom resource state indicating the form is waiting for user input.
    /// </summary>
    public const string WaitingForConfigurationState = "WaitingForConfiguration";

    /// <summary>
    /// Creates a new StartupFormResource.
    /// </summary>
    /// <param name="name">Resource name (typically "{parentName}-config").</param>
    /// <param name="form">The startup form definition from the manifest.</param>
    /// <param name="parentProject">The project resource this form belongs to.</param>
    public StartupFormResource(string name, StartupFormDefinition form, IResource parentProject)
        : base(name)
    {
        Form = form ?? throw new ArgumentNullException(nameof(form));
        ParentProject = parentProject ?? throw new ArgumentNullException(nameof(parentProject));
    }

    /// <summary>
    /// The startup form definition containing title, description, and inputs.
    /// </summary>
    public StartupFormDefinition Form { get; }

    /// <summary>
    /// The project resource this startup form belongs to.
    /// </summary>
    public IResource ParentProject { get; }

    /// <summary>
    /// Whether the startup form has been completed by the user.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// The form data submitted by the user, keyed by input name.
    /// Only populated after <see cref="MarkCompleted"/> is called.
    /// </summary>
    public Dictionary<string, string?> FormData { get; private set; } = new();

    /// <summary>
    /// Marks the startup form as completed with the provided form data.
    /// </summary>
    /// <param name="formData">The form data submitted by the user.</param>
    internal void MarkCompleted(Dictionary<string, string?> formData)
    {
        FormData = formData ?? throw new ArgumentNullException(nameof(formData));
        IsCompleted = true;
    }
}
