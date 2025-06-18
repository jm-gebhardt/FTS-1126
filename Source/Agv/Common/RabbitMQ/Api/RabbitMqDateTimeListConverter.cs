using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.Common.RabbitMQ.Api;

/// <summary>
/// JSON converter for a list of DateTime objects used in RabbitMQ management API responses.
/// </summary>
public class RabbitMqDateTimeListConverter : JsonConverter<List<DateTime>>
{
    private const string Format = "yyyy-MM-dd H:mm:ss";

    /// <inheritdoc />
    public override List<DateTime> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new List<DateTime>();

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected array");
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            string? str = reader.GetString();
            if (DateTime.TryParseExact(str, Format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out var utc))
            {
                result.Add(utc);
            }
            else
            {
                throw new JsonException($"Invalid datetime format: {str}");
            }
        }

        return result;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, List<DateTime> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var dt in value)
        {
            writer.WriteStringValue(dt.ToString(Format));
        }
        writer.WriteEndArray();
    }
}