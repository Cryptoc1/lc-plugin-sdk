using Microsoft.Build.Framework;

namespace LethalCompany.Plugin.Sdk.Internal;

internal sealed record ThunderDependency(ThunderDependencyMoniker Moniker) : IEquatable<string>
{
    /// <summary> Glob patterns of plugin assets that shouldn't be referenced. </summary>
    /// <remarks> May be used to exclude DLLs from being referenced. </remarks>
    public string[] ExcludeAssets { get; init; } = [];

    /// <summary> Glob patterns of plugin assets that should be referenced. </summary>
    /// <remarks> May be used to include DLLs to be referenced. </remarks>
    public string[] IncludeAssets { get; init; } = ["*.dll", @"plugins\**\*.dll", @"BepInEx\core\**\*.dll", @"BepInEx\plugins\**\*.dll;"];

    public static ThunderDependency From(ITaskItem item)
    {
        return new(ThunderDependencyMoniker.From(item))
        {
            ExcludeAssets = AssetPatterns(item, nameof(ExcludeAssets)),
            IncludeAssets = AssetPatterns(item, nameof(IncludeAssets)),
        };

        static string[] AssetPatterns(ITaskItem item, string name) => [
            ..item.GetMetadata(name)
                ?.Split([";"], StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Distinct()];
    }

    public bool Equals(string? other) => Moniker.Value.Equals(other, StringComparison.Ordinal);

    public static implicit operator string(ThunderDependency dependency) => dependency.Moniker;
}

internal sealed record class ThunderDependencyMoniker(string Name, string Team, SemanticVersion Version) : IEquatable<string>
{
    public readonly string FullName = $"{Team}-{Name}";

    public readonly string Value = $"{Team}-{Name}-{Version}";

    public bool Equals(string? other) => Value.Equals(Value, StringComparison.Ordinal);

    public static ThunderDependencyMoniker From(ITaskItem item)
    {
        var names = item.ItemSpec.Split('-');
        if (names.Length < 2) throw new ArgumentException($"Thunderstore identifier '{item.ItemSpec}' is not valid.", nameof(item));

        return new(
            names[1],
            names[0],
            SemanticVersion.Parse(names.Length is 3 ? names[2] : item.GetMetadata(nameof(Version))));
    }

    public override int GetHashCode() => Value.GetHashCode();

    public static ThunderDependencyMoniker Parse(string identity)
    {
        var names = identity.Split('-');
        if (names.Length is not 3) throw new ArgumentException($"Thunderstore identifier '{identity}' is not valid.", nameof(identity));

        return new(names[1], names[0], SemanticVersion.Parse(names[2]));
    }

    public static ThunderDependencyMoniker Parse(string identity, SemanticVersion version)
    {
        var names = identity.Split('-');
        if (names.Length is not 2) throw new ArgumentException($"Thunderstore identifier '{identity}' is not valid.", nameof(identity));

        return new(names[1], names[0], version);
    }

    public override string ToString() => Value;

    public static implicit operator string(ThunderDependencyMoniker moniker) => moniker.Value;
}
