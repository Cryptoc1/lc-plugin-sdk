using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using LethalCompany.Plugin.Sdk.Internal;
using Microsoft.Build.Framework;
using Microsoft.Extensions.FileSystemGlobbing;

namespace LethalCompany.Plugin.Sdk;

/// <summary> Restores the given <see cref="Dependencies"/> of a plugin. </summary>
public sealed class RestorePluginDependencies : Microsoft.Build.Utilities.Task
{
    /// <summary> The absolute path to the Thunderstore plugin cache directory. </summary>
    [Required]
    public string CacheDirectory { get; init; } = default!;

    /// <summary> The <c>@(ThunderDependency)</c> items. </summary>
    [Required]
    public ITaskItem[] Dependencies { get; init; } = [];

    /// <summary> The generated json text of the restored plugin assets. </summary>
    [Output]
    public string GeneratedAssetsJson { get; set; } = string.Empty;

    /// <summary> Whether plugins should be restored in locked mode. </summary>
    public bool LockedMode { get; init; }

    /// <summary> The absolute path to the json file that stores serialized <see cref="PluginDependencyGraph"/>. </summary>
    public string PluginLockFile { get; init; } = string.Empty;

    /// <summary> Whether the <see cref="PluginDependencyGraph"/> should be written to the <see cref="PluginLockFile"/>. </summary>
    public bool WithLockFile { get; init; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        using var cache = new PluginCache(CacheDirectory);
        var dependencies = Dependencies.Select(ThunderDependency.From);

        var status = TryResolve(cache, dependencies, out var graph);
        if (status > ResolutionStatus.Failed && graph is not null && TryRestore(cache, graph, out var restorations))
        {
            if (WithLockFile && status is ResolutionStatus.Resolved)
            {
                File.WriteAllText(
                    PluginLockFile,
                    JsonSerializer.Serialize(graph, SdkJsonContext.Default.PluginDependencyGraph),
                    Encoding.UTF8);
            }

            GeneratedAssetsJson = JsonSerializer.Serialize(
                EvaluateAssets(dependencies, restorations),
                SdkJsonContext.Default.PluginAssetsCollection);

            return true;
        }

        return false;
    }

    private static PluginAssetsCollection EvaluateAssets(IEnumerable<ThunderDependency> dependencies, PluginRestoration[] restorations)
    {
        var assets = new PluginAssetsCollection();
        foreach (var (dependency, path) in Merge(dependencies, restorations))
        {
            var matcher = new Matcher();
            matcher.AddExcludePatterns(dependency.ExcludeAssets);
            matcher.AddIncludePatterns(dependency.IncludeAssets);

            assets.Add(
                dependency.Moniker,
                [.. matcher.GetResultsInFullPath(path)]);
        }

        return assets;

        static IEnumerable<(ThunderDependency Dependency, string Path)> Merge(IEnumerable<ThunderDependency> dependencies, PluginRestoration[] restorations)
        {
            var direct = dependencies.ToDictionary(dependency => dependency.Moniker.FullName);
            foreach (var restoration in restorations)
            {
                // NOTE: ignore failed restorations
                if (restoration is not PluginRestoration.Success success) continue;

                // NOTE: use the original dependency for asset inclusion/exclusions; always use the restored version
                if (direct.TryGetValue(restoration.Moniker.FullName, out var dependency))
                {
                    yield return (dependency with { Moniker = restoration.Moniker }, success.Path);
                    continue;
                }

                yield return (new(restoration.Moniker), success.Path);
            }
        }
    }

    private static bool TryReadLockFile(string path, [NotNullWhen(true)] out PluginDependencyGraph? graph)
    {
        if (File.Exists(path))
        {
            using var file = File.OpenRead(path);
            return (graph = JsonSerializer.Deserialize(file, SdkJsonContext.Default.PluginDependencyGraph)) is not null;
        }

        graph = default;
        return false;
    }

    private ResolutionStatus TryResolve(PluginCache cache, IEnumerable<ThunderDependency> dependencies, out PluginDependencyGraph? graph)
    {
        if (LockedMode)
        {
            if (!TryReadLockFile(PluginLockFile, out graph))
            {
                Log.LogError($"Cannot restore plugins in LockedMode, the lock file '{PluginLockFile}' does not exist. You must include '<RestorePluginsWithLockFile>true</RestorePluginsWithLockFile>' in your project file to generate this file.");
                return ResolutionStatus.Failed;
            }

            Log.LogMessage($"Restoring plugins using lock file '{PluginLockFile}'.");
            return ResolutionStatus.Locked;
        }

        var monikers = dependencies.Select(dependency => dependency.Moniker);
        if (WithLockFile && TryReadLockFile(PluginLockFile, out var locked))
        {
            // NOTE: detect changes by performing a full-outer-join between current monikers and locked monikers
            if (!locked.Dependencies.Where(entry => entry.Value.Type is PluginDependencyType.Direct)
                .Select(entry => ThunderDependencyMoniker.Parse(entry.Key, entry.Value.Requested!))
                .FullJoin(monikers, moniker => moniker, moniker => moniker, (locked, requested) => (requested, locked))
                .Any(value => value.requested is null || value.locked is null))
            {
                Log.LogMessage("Skipped resolving dependency graph, the lock file is up-to-date.");

                graph = locked;
                return ResolutionStatus.Locked;
            }

            // TODO: progressive resolution; update the locked graph with missing dependencies, rather than re-resolving the entire graph
            Log.LogMessage("Resolving dependency graph, the lock file is not up-to-date.");
        }

        var resolution = cache.Resolve(monikers);
        if (resolution.Failed.Length is not 0)
        {
            foreach (var failure in resolution.Failed)
            {
                Log.LogWarning($"Failed to resolve plugin '{failure.Moniker}'.");
            }
        }

        graph = resolution.Graph;
        return ResolutionStatus.Resolved;
    }

    private bool TryRestore(PluginCache cache, PluginDependencyGraph graph, [NotNull] out PluginRestoration[] restorations)
    {
        if (graph.Dependencies.Count is 0)
        {
            restorations = [];
            return true;
        }

        var restored = true;
        restorations = cache.Restore(graph.Dependencies.Select(
            entry => ThunderDependencyMoniker.Parse(entry.Key, entry.Value.Resolved)));

        foreach (var restoration in restorations)
        {
            if (restoration is PluginRestoration.Skipped)
            {
                Log.LogMessage($"Skipped restoring '{restoration.Moniker}', it already exists in the cache.");
                continue;
            }

            if (restoration is PluginRestoration.Failure failure)
            {
                Log.LogError($"Failed to restore '{restoration.Moniker}': {failure.Message}");
                if (failure.Exception is not null)
                {
                    Log.LogErrorFromException(failure.Exception);
                }

                restored = false;
                continue;
            }

            if (restoration is PluginRestoration.Success success)
            {
                Log.LogMessage($"Restored '{success.Moniker}' to '{success.Path}'.");
            }
        }

        return restored;
    }

    private enum ResolutionStatus
    {
        Failed,
        Locked,
        Resolved
    }
}
