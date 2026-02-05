#pragma warning disable ASPIREINTERACTION001

using System.Text.Json;
using Aspire.Hosting;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Reads and parses project command manifest files.
/// </summary>
internal static class ManifestReader
{
    /// <summary>
    /// The expected manifest file name in the project directory.
    /// </summary>
    public const string ManifestFileName = "projectcommander.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Attempts to read a project command manifest from the specified project directory.
    /// </summary>
    /// <param name="projectDirectory">The directory containing the project.</param>
    /// <returns>The parsed manifest, or null if no manifest file exists.</returns>
    public static ProjectCommandManifest? ReadManifest(string projectDirectory)
    {
        var manifestPath = Path.Combine(projectDirectory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<ProjectCommandManifest>(json, JsonOptions);
    }

    /// <summary>
    /// Converts an InputDefinition from the manifest to an Aspire InteractionInput.
    /// </summary>
    /// <param name="definition">The input definition from the manifest.</param>
    /// <returns>An Aspire InteractionInput configured according to the definition.</returns>
    public static InteractionInput ToInteractionInput(InputDefinition definition)
    {
        var input = new InteractionInput
        {
            Name = definition.Name,
            Label = definition.Label ?? definition.Name,
            Description = definition.Description,
            InputType = ParseInputType(definition.InputType),
            Required = definition.Required,
            Placeholder = definition.Placeholder,
            MaxLength = definition.MaxLength,
            AllowCustomChoice = definition.AllowCustomChoice,
            EnableDescriptionMarkdown = definition.EnableDescriptionMarkdown
        };

        // Set options for Choice input type
        if (definition.Options is { Count: > 0 })
        {
            // Convert string options to KeyValuePair where key and value are the same
            input.Options = definition.Options
                .Select(o => new KeyValuePair<string, string>(o, o))
                .ToList();
        }

        return input;
    }

    /// <summary>
    /// Parses an input type string from the manifest to the Aspire InputType enum.
    /// </summary>
    /// <param name="inputTypeString">The input type string (e.g., "Text", "Number").</param>
    /// <returns>The corresponding InputType enum value.</returns>
    /// <exception cref="ArgumentException">Thrown when the input type string is not recognized.</exception>
    private static InputType ParseInputType(string inputTypeString)
    {
        return inputTypeString.ToLowerInvariant() switch
        {
            "text" => InputType.Text,
            "secrettext" => InputType.SecretText,
            "choice" => InputType.Choice,
            "boolean" => InputType.Boolean,
            "number" => InputType.Number,
            _ => throw new ArgumentException($"Unknown input type: {inputTypeString}. Valid types are: Text, SecretText, Choice, Boolean, Number.", nameof(inputTypeString))
        };
    }
}
