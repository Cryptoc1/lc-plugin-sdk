using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LethalCompany.Plugin.Sdk.Internal;

internal sealed class PluginCache(string directory) : IDisposable
{
    private readonly PluginCacheServices Services = new();

    public string CacheDirectory { get; } = directory;

    public void Dispose() => Services.Dispose();

    public PluginDownload Download(ThunderDependencyMoniker moniker)
        => DownloadAsync(moniker)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    public async Task<PluginDownload> DownloadAsync(ThunderDependencyMoniker moniker)
    {
        if (TryGetPackagePath(moniker, out var path))
        {
            return new PluginDownload.Skipped(moniker, path);
        }

        try
        {
            using var response = await Services.Http.GetAsync(
                $"https://thunderstore.io/package/download/{moniker.Team}/{moniker.Name}/{moniker.Version}/").ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.NotFound) return new PluginDownload.Failure(moniker, "Package was not found on Thunderstore.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            try
            {
                using var package = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write);

                await response.Content.CopyToAsync(package).ConfigureAwait(false);
                await package.FlushAsync().ConfigureAwait(false);
            }
            catch
            {
                if (File.Exists(path)) File.Delete(path);
                throw;
            }

            return new PluginDownload.Success(moniker, path);
        }
        catch (Exception exception)
        {
            return new PluginDownload.Failure(moniker, "Unable to download package.")
            {
                Exception = exception
            };
        }
    }

    public async ValueTask<PluginMetadata?> QueryAsync(ThunderDependencyMoniker moniker)
    {
        if (Services.Cache.TryGetValue<PluginMetadata>(moniker, out var metadata))
        {
            return metadata;
        }

        using var response = await Services.Http.GetAsync(
            $"https://thunderstore.io/api/experimental/package/{moniker.Team}/{moniker.Name}/{moniker.Version}")
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        metadata = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync(ThunderstoreJsonContext.Default.PluginMetadata)
            .ConfigureAwait(false);

        return Services.Cache.Set(
            moniker,
            metadata,
            new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(5)));
    }

    public PluginResolution Resolve(IEnumerable<ThunderDependencyMoniker> moniker)
        => ResolveAsync(moniker)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    public async Task<PluginResolution> ResolveAsync(IEnumerable<ThunderDependencyMoniker> monikers)
    {
        var failures = new List<PluginResolutionFailure>();
        var graph = new PluginDependencyGraph();

        foreach (var moniker in monikers)
        {
            var dependency = await ResolveDirect(moniker).ConfigureAwait(false);
            if (dependency is not null)
            {
                graph.Dependencies.Add(moniker.FullName, dependency);
                continue;
            }

            failures.Add(new PluginResolutionFailure(moniker));
        }

        var buffer = new List<(string, PluginDependencyNode)>();
        var source = graph.Dependencies.Values.ToList();

        while (await ResolveTransitives(source, buffer, failures).ConfigureAwait(false))
        {
            source.Clear();
            foreach (var (name, dependency) in buffer)
            {
                if (!graph.Dependencies.TryGetValue(name, out var existing))
                {
                    graph.Dependencies.Add(name, dependency);
                    continue;
                }

                // NOTE: direct deps take precedence
                if (existing.Type is PluginDependencyType.Direct || existing.Resolved == dependency.Resolved) continue;

                // NOTE: take the newer version
                if (existing.Resolved < dependency.Resolved)
                {
                    graph.Dependencies[name] = dependency;
                }

                source.Add(dependency);
            }

            buffer.Clear();
        }

        return new(graph)
        {
            Failed = [.. failures],
        };

        async Task<PluginDependencyNode?> ResolveDirect(ThunderDependencyMoniker moniker)
        {
            var metadata = await QueryAsync(moniker).ConfigureAwait(false);
            if (metadata is null) return null;

            return PluginDependencyNode.From(metadata, PluginDependencyType.Direct) with
            {
                Requested = moniker.Version
            };
        }

        async Task<PluginDependencyNode?> ResolveTransitive(ThunderDependencyMoniker moniker)
        {
            var metadata = await QueryAsync(moniker).ConfigureAwait(false);
            if (metadata is null) return null;

            return PluginDependencyNode.From(metadata, PluginDependencyType.Transitive);
        }

        async Task<bool> ResolveTransitives(IReadOnlyList<PluginDependencyNode> source, IList<(string, PluginDependencyNode)> resolved, IList<PluginResolutionFailure> failures)
        {
            resolved.Clear();
            foreach (var value in source)
            {
                if (value.Dependencies?.Count is null or 0) continue;
                foreach (var moniker in value.Dependencies.Select(item => ThunderDependencyMoniker.Parse(item.Key, item.Value)))
                {
                    var transitive = await ResolveTransitive(moniker).ConfigureAwait(false);
                    if (transitive is null)
                    {
                        failures.Add(new PluginResolutionFailure(moniker));
                        continue;
                    }

                    resolved.Add((moniker.FullName, transitive));
                }
            }

            return resolved.Count > 0;
        }
    }

    public async Task<PluginRestoration> RestoreAsync(ThunderDependencyMoniker moniker)
    {
        var directory = Path.Combine(CacheDirectory, moniker.FullName, moniker.Version.ToString());
        if (File.Exists(
            Path.Combine(directory, "manifest.json")))
        {
            return new PluginRestoration.Skipped(moniker, directory);
        }

        var download = await DownloadAsync(moniker).ConfigureAwait(false);
        if (download is PluginDownload.Failure failure)
        {
            return new PluginRestoration.Failure(moniker, failure.Message)
            {
                Exception = failure.Exception,
            };
        }

        try
        {
            Directory.CreateDirectory(directory);
            ZipFile.ExtractToDirectory(download.Path!, directory);

            return new PluginRestoration.Success(moniker, directory);
        }
        catch (Exception exception)
        {
            return new PluginRestoration.Failure(moniker, "Error while extracting package.")
            {
                Exception = exception,
            };
        }
    }

    public PluginRestoration[] Restore(IEnumerable<ThunderDependencyMoniker> monikers)
        => RestoreAsync(monikers)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    public async Task<PluginRestoration[]> RestoreAsync(IEnumerable<ThunderDependencyMoniker> monikers)
    {
        using var semaphore = new SemaphoreSlim(3, 5);
        return await Task.WhenAll(
            monikers.Select(async moniker =>
            {
                await semaphore.WaitAsync();

                try
                {
                    return await RestoreAsync(moniker);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
    }

    private bool TryGetPackagePath(ThunderDependencyMoniker moniker, [NotNull] out string path)
    {
        path = Path.Combine(CacheDirectory, moniker.FullName, $"{moniker}.zip");
        return File.Exists(path);
    }

    private sealed class PluginCacheServices : IDisposable
    {
        private MemoryCache? cache;
        private HttpClient? http;

        public MemoryCache Cache => cache ??= new(
            Options.Create<MemoryCacheOptions>(new()));

        public HttpClient Http => http ??= new()
        {
            DefaultRequestHeaders = {
                { "User-Agent", $"{SdkInfo.AssemblyName}/{SdkInfo.Version}" }
            }
        };

        public void Dispose()
        {
            cache?.Dispose();
            http?.Dispose();
        }
    }
}

internal abstract record PluginDownload(ThunderDependencyMoniker Moniker, string? Path)
{
    public sealed record Failure(ThunderDependencyMoniker Moniker, string Message, string? Path = null) : PluginDownload(Moniker, Path)
    {
        public Exception? Exception { get; init; }
    }

    public sealed record Skipped(ThunderDependencyMoniker Moniker, string Path) : PluginDownload(Moniker, Path);
    public sealed record Success(ThunderDependencyMoniker Moniker, string Path) : PluginDownload(Moniker, Path);
}

internal sealed record PluginResolution(PluginDependencyGraph Graph)
{
    public PluginResolutionFailure[] Failed { get; init; } = [];
}

internal sealed record PluginResolutionFailure(ThunderDependencyMoniker Moniker);

internal abstract record PluginRestoration(ThunderDependencyMoniker Moniker)
{
    public sealed record Failure(ThunderDependencyMoniker Moniker, string Message) : PluginRestoration(Moniker)
    {
        public Exception? Exception { get; init; }
    }

    public sealed record Skipped(ThunderDependencyMoniker Moniker, string Path) : Success(Moniker, Path);
    public record Success(ThunderDependencyMoniker Moniker, string Path) : PluginRestoration(Moniker);
}
