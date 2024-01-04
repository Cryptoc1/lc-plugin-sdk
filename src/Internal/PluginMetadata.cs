using System.Text.Json.Serialization;

namespace LethalCompany.Plugin.Sdk.Internal;

internal sealed record class PluginMetadata
{
    [JsonPropertyName("date_created")]
    public DateTime DateCreated { get; init; }

    public string[] Dependencies { get; init; } = [];

    public string Description { get; init; } = default!;

    [JsonPropertyName("download_url")]
    public Uri DownloadUrl { get; init; } = default!;

    public uint Downloads { get; init; }

    [JsonPropertyName("icon")]
    public Uri IconUrl { get; init; } = default!;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = default!;

    public string Namespace { get; init; } = default!;

    public string Name { get; init; } = default!;

    [JsonPropertyName("version_number")]
    public SemanticVersion Version { get; init; } = default!;

    [JsonPropertyName("website_url")]
    public Uri WebsiteUrl { get; init; } = default!;
}
