using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using LethalCompany.Plugin.Sdk.Internal;
using Microsoft.Build.Framework;

namespace LethalCompany.Plugin.Sdk;

/// <summary> Resolves a plugin's reference assemblies. </summary>
/// <remarks> Requires that <see cref="RestorePluginDependencies"/> has been run, and it's <see cref="RestorePluginDependencies.GeneratedAssetsJson"/> has been written to the given <see cref="AssetsFile"/>. </remarks>
public sealed class ResolvePluginAssemblies : Microsoft.Build.Utilities.Task
{
    /// <summary> The absolute path to a file containing the serialized <see cref="PluginAssetsCollection"/>. </summary>
    [Required]
    public string AssetsFile { get; init; } = string.Empty;

    /// <summary> The absolute paths of the resolved plugin assemblies. </summary>
    [Output]
    public string[] ResolvedAssemblies { get; set; } = [];

    /// <inheritdoc/>
    public override bool Execute()
    {
        if (!TryReadAssetsFile(AssetsFile, out var assets))
        {
            Log.LogError($"Failed to resolve plugin assemblies, the assets file '{AssetsFile}' could not be read.");
            return false;
        }

        ResolvedAssemblies = [.. assets.Values.SelectMany(paths => paths)];
        return true;
    }

    private static bool TryReadAssetsFile(string path, [NotNullWhen(true)] out PluginAssetsCollection? assets)
    {
        if (File.Exists(path))
        {
            using var file = File.OpenRead(path);
            return (assets = JsonSerializer.Deserialize(file, SdkJsonContext.Default.PluginAssetsCollection)) is not null;
        }

        assets = default;
        return false;
    }
}
