using System.Text.Json;
using System.Text.Json.Serialization;
using WebBundler.Core;

namespace WebBundler.Configuration;

public sealed class BundleTypeJsonConverter : JsonConverter<BundleType>
{
    public override BundleType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.Trim().ToLowerInvariant() switch
        {
            "css" => BundleType.Css,
            "js" or "javascript" => BundleType.JavaScript,
            _ => throw new JsonException($"Unsupported bundle type '{value}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, BundleType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            BundleType.Css => "css",
            BundleType.JavaScript => "js",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        });
    }
}
