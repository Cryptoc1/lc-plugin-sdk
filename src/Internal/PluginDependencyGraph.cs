using System.Text.Json.Serialization;

namespace LethalCompany.Plugin.Sdk.Internal;

internal sealed record class PluginDependencyGraph
{
    public double Version { get; } = 1.0;

    [JsonPropertyOrder(1)]
    public Dictionary<string, PluginDependencyNode> Dependencies { get; init; } = [];
}

internal sealed record class PluginDependencyNode
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(10)]
    public Dictionary<string, SemanticVersion>? Dependencies { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(1)]
    public SemanticVersion? Requested { get; init; }

    [JsonPropertyOrder(2)]
    public required SemanticVersion Resolved { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<PluginDependencyType>))]
    public required PluginDependencyType Type { get; init; }

    public static PluginDependencyNode From(PluginMetadata metadata, PluginDependencyType type)
    {
        var node = new PluginDependencyNode
        {
            Resolved = metadata.Version,
            Type = type,
        };

        if (metadata.Dependencies?.Length > 0)
        {
            node = node with
            {
                Dependencies = metadata.Dependencies.Select(ThunderDependencyMoniker.Parse).ToDictionary(
                    moniker => moniker.FullName, moniker => moniker.Version)
            };
        }

        return node;
    }
}

internal enum PluginDependencyType
{
    Direct,
    Transitive,
}
