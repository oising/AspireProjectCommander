namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Default implementation of resource name parser.
/// </summary>
internal sealed class ResourceNameParser : IResourceNameParser
{
    /// <inheritdoc />
    public string GetBaseResourceName(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            throw new ArgumentException("Resource name cannot be null or empty.", nameof(resourceName));
        }

        // Split on last hyphen to extract base name
        // Example: "datagenerator-abc123" -> "datagenerator"
        if (!resourceName.Contains('-'))
        {
            return resourceName;
        }

        var baseName = resourceName[..resourceName.LastIndexOf("-", StringComparison.Ordinal)];
        return baseName;
    }
}
