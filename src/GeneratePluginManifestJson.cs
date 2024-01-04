using System.Text.Json;
using LethalCompany.Plugin.Sdk.Internal;
using Microsoft.Build.Framework;

namespace LethalCompany.Plugin.Sdk;

/// <summary> Generates the JSON text of a Thunderstore manifest. </summary>
public sealed class GeneratePluginManifestJson : Microsoft.Build.Utilities.Task
{
    /// <summary> The <c>@(ThunderDependency)</c> items to write to the generated json. </summary>
    [Required]
    public ITaskItem[] Dependencies { get; init; } = [];

    /// <summary> The <c>description</c> to write to the generated json. </summary>
    [Required]
    public string Description { get; init; } = string.Empty;

    /// <summary> The json text that was generated. </summary>
    [Output]
    public string GeneratedText { get; set; } = string.Empty;

    /// <summary> The <c>name</c> to write to the generated json. </summary>
    [Required]
    public string Name { get; init; } = string.Empty;

    /// <summary> The <c>version_number</c> to write to the generated json. </summary>
    [Required]
    public string Version { get; init; } = string.Empty;

    /// <summary> The <c>website_url</c> to write to the generated json. </summary>
    public string WebsiteUrl { get; init; } = string.Empty;

    /// <inheritdoc/>
    public override bool Execute()
    {
        var manifest = new PluginManifest
        {
            Dependencies = [.. Dependencies.Select(ThunderDependencyMoniker.From)],
            Description = Description,
            Name = Name,
            Version = SemanticVersion.Parse(Version),
            WebsiteUrl = WebsiteUrl,
        };

        if (!manifest.TryValidate(out var errors))
        {
            foreach (var error in errors)
            {
                Log.LogWarning($"Thunderstore Manifest Validation: {error.ErrorMessage}");
            }
        }

        GeneratedText = JsonSerializer.Serialize(manifest, ThunderstoreJsonContext.Default.PluginManifest);
        return true;
    }
}
