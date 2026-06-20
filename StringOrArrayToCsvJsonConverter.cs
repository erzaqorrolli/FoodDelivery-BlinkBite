using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodDeliveryyy.Models.Converters;

public sealed class StringOrArrayToCsvJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return string.Empty;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var parts = new List<string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        parts.Add(value.Trim());
                    }
                }
                else
                {
                    // Skip non-string values to keep payload tolerance high.
                    using var _ = JsonDocument.ParseValue(ref reader);
                }
            }

            return string.Join(", ", parts);
        }

        throw new JsonException($"Unexpected token {reader.TokenType} for string or string[] field.");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ?? string.Empty);
    }
}
