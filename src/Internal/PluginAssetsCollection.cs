using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LethalCompany.Plugin.Sdk.Internal;

[JsonConverter(typeof(PluginAssetsCollectionJsonConverter))]
internal sealed class PluginAssetsCollection(Dictionary<ThunderDependencyMoniker, string[]>? source = null) : IDictionary<ThunderDependencyMoniker, string[]>
{
    private readonly IDictionary<ThunderDependencyMoniker, string[]> source = source ?? [];

    public string[] this[ThunderDependencyMoniker key] { get => source[key]; set => source[key] = value; }

    public ICollection<ThunderDependencyMoniker> Keys => source.Keys;
    public ICollection<string[]> Values => source.Values;
    public int Count => source.Count;
    public bool IsReadOnly => source.IsReadOnly;

    public void Add(ThunderDependencyMoniker key, string[] value) => source.Add(key, value);

    public void Add(KeyValuePair<ThunderDependencyMoniker, string[]> item) => source.Add(item);

    public void Clear() => source.Clear();

    public bool Contains(KeyValuePair<ThunderDependencyMoniker, string[]> item) => source.Contains(item);

    public bool ContainsKey(ThunderDependencyMoniker key) => source.ContainsKey(key);

    public void CopyTo(KeyValuePair<ThunderDependencyMoniker, string[]>[] array, int arrayIndex) => source.CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<ThunderDependencyMoniker, string[]>> GetEnumerator() => source.GetEnumerator();

    public bool Remove(ThunderDependencyMoniker key) => source.Remove(key);

    public bool Remove(KeyValuePair<ThunderDependencyMoniker, string[]> item) => source.Remove(item);

#pragma warning disable CS8767
    public bool TryGetValue(ThunderDependencyMoniker key, [MaybeNullWhen(false)] out string[] value) => source.TryGetValue(key, out value);
#pragma warning restore CS8767

    IEnumerator IEnumerable.GetEnumerator() => source.GetEnumerator();
}

internal sealed class PluginAssetsCollectionJsonConverter : JsonConverter<PluginAssetsCollection>
{
    public override PluginAssetsCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var assets = JsonSerializer.Deserialize<Dictionary<string, string[]>>(ref reader, options);
        return new(
            assets?.ToDictionary(asset => ThunderDependencyMoniker.Parse(asset.Key), asset => asset.Value));
    }

    public override void Write(Utf8JsonWriter writer, PluginAssetsCollection value, JsonSerializerOptions options)
    {
        var assets = value.ToDictionary(
            asset => asset.Key.Value,
            asset => asset.Value);

        JsonSerializer.Serialize(writer, assets, options);
    }
}
