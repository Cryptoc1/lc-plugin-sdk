using System.Text.Json;
using System.Text.Json.Serialization;

namespace LethalCompany.Plugin.Sdk.Internal;

[JsonSourceGenerationOptions(
    Converters = [typeof(PluginAssetsCollectionJsonConverter), typeof(SemanticVersionJsonConverter)],
    DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSerializable(typeof(PluginAssetsCollection))]
[JsonSerializable(typeof(PluginDependencyGraph))]
[JsonSerializable(typeof(PluginDependencyNode))]
internal sealed partial class SdkJsonContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    Converters = [typeof(SemanticVersionJsonConverter)],
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(PluginManifest))]
[JsonSerializable(typeof(PluginMetadata))]
internal sealed partial class ThunderstoreJsonContext : JsonSerializerContext
{
}

internal sealed class SemanticVersionJsonConverter : JsonConverter<SemanticVersion>
{
    public override SemanticVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => SemanticVersion.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
