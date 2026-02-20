using System.Text.Json.Serialization;

namespace CommunityToolkit.Aspire.Hosting.ProjectCommander;

/// <summary>
/// Represents a project command manifest loaded from projectcommander.json.
/// This manifest defines startup forms and commands that projects can surface in the Aspire dashboard.
/// </summary>
public sealed class ProjectCommandManifest
{
    /// <summary>
    /// The version of the manifest schema. Currently only "1.0" is supported.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Optional startup form that must be filled out before the project starts its main work.
    /// </summary>
    [JsonPropertyName("startupForm")]
    public StartupFormDefinition? StartupForm { get; set; }

    /// <summary>
    /// Commands that should be added to the Aspire dashboard for this project.
    /// </summary>
    [JsonPropertyName("commands")]
    public List<CommandDefinition> Commands { get; set; } = [];
}

/// <summary>
/// Defines a startup form that blocks project execution until filled out by the developer.
/// </summary>
public sealed class StartupFormDefinition
{
    /// <summary>
    /// The title of the startup form dialog.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>
    /// Optional description text for the startup form.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The input fields for the startup form.
    /// </summary>
    [JsonPropertyName("inputs")]
    public List<InputDefinition> Inputs { get; set; } = [];
}

/// <summary>
/// Defines a command that appears in the Aspire dashboard for this project.
/// </summary>
public sealed class CommandDefinition
{
    /// <summary>
    /// The unique identifier for the command. Must be lowercase alphanumeric with hyphens.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The display name shown in the Aspire dashboard.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Optional description of what the command does.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The icon name to display. Uses Fluent UI icon names.
    /// </summary>
    [JsonPropertyName("iconName")]
    public string? IconName { get; set; }

    /// <summary>
    /// The icon variant: "Regular" or "Filled".
    /// </summary>
    [JsonPropertyName("iconVariant")]
    public string? IconVariant { get; set; }

    /// <summary>
    /// Optional confirmation message to show before executing the command.
    /// </summary>
    [JsonPropertyName("confirmationMessage")]
    public string? ConfirmationMessage { get; set; }

    /// <summary>
    /// Whether the command should be highlighted in the dashboard.
    /// </summary>
    [JsonPropertyName("isHighlighted")]
    public bool IsHighlighted { get; set; }

    /// <summary>
    /// Optional input fields to prompt for when the command is executed.
    /// </summary>
    [JsonPropertyName("inputs")]
    public List<InputDefinition> Inputs { get; set; } = [];
}

/// <summary>
/// Defines an input field for a command or startup form.
/// Maps to Aspire's InteractionInput type.
/// </summary>
public sealed class InputDefinition
{
    /// <summary>
    /// The unique name of the input field.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The label displayed to the user.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// Optional description or help text for the input.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The type of input: "Text", "SecretText", "Choice", "Boolean", or "Number".
    /// </summary>
    [JsonPropertyName("inputType")]
    public required string InputType { get; set; }

    /// <summary>
    /// Whether this input is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>
    /// Placeholder text for the input field.
    /// </summary>
    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Maximum length for text inputs.
    /// </summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Options for Choice input types.
    /// </summary>
    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    /// <summary>
    /// Whether custom choices are allowed (for Choice input type).
    /// </summary>
    [JsonPropertyName("allowCustomChoice")]
    public bool AllowCustomChoice { get; set; }

    /// <summary>
    /// Whether the description supports Markdown rendering.
    /// </summary>
    [JsonPropertyName("enableDescriptionMarkdown")]
    public bool EnableDescriptionMarkdown { get; set; }
}
