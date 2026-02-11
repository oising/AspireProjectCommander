namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Service for parsing Aspire resource names.
/// </summary>
public interface IResourceNameParser
{
    /// <summary>
    /// Extracts the base resource name from a full resource name that may include a suffix.
    /// Example: "datagenerator-abc123" -> "datagenerator"
    /// </summary>
    /// <param name="resourceName">The full resource name.</param>
    /// <returns>The base resource name without suffix.</returns>
    string GetBaseResourceName(string resourceName);
}
