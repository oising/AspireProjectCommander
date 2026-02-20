using Aspire.Hosting.ApplicationModel;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Annotation applied to a project resource to link it to its associated <see cref="StartupFormResource"/>.
/// This allows the SignalR hub to find the form resource when a project connects.
/// </summary>
public sealed class StartupFormResourceAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Creates a new StartupFormResourceAnnotation.
    /// </summary>
    /// <param name="startupFormResource">The startup form resource for this project.</param>
    public StartupFormResourceAnnotation(StartupFormResource startupFormResource)
    {
        StartupFormResource = startupFormResource ?? throw new ArgumentNullException(nameof(startupFormResource));
    }

    /// <summary>
    /// The startup form resource associated with the parent project.
    /// </summary>
    public StartupFormResource StartupFormResource { get; }
}
