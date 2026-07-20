using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageShare.Browsing;

public sealed class RelativePathJsonConverter : JsonConverter<RelativePath>
{
    public override RelativePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return RelativePath.TryParse(value, out var path) ? path : throw new JsonException($"Invalid relative path: {value}");
    }

    public override void Write(Utf8JsonWriter writer, RelativePath value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
