using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.Common.Messages.Converter;

/// <summary>
/// Provides a converter for parameters of instant actions.
/// </summary>
public class InstantActionParameterJsonConverter :JsonConverter<Dictionary<string, object>>
{
    /// <inheritdoc cref="JsonConverter"/>>
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("JSON object expected.");
        }

        var dictionary = new Dictionary<string, object>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Property name expected.");
            }

            string propertyName = reader.GetString()!;
            reader.Read();

            dictionary[propertyName] = ReadValue(ref reader, options);
        }

        throw new JsonException("JSON object not properly closed.");
    }

    private object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString()!;

            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int intValue))
                {
                    return intValue;
                }
                return reader.GetDouble();

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.StartArray:
                var list = new List<object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(ReadValue(ref reader, options));
                }
                return list.ToArray();

            case JsonTokenType.StartObject:
                return Read(ref reader, typeof(Dictionary<string, object>), options);

            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }
    }

    /// <inheritdoc cref="JsonConverter"/>>
    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            WriteValue(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }

    private void WriteValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;

            case int intValue:
                writer.WriteNumberValue(intValue);
                break;

            case float floatValue:
                writer.WriteNumberValue(floatValue);
                break;

            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;

            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;

            case Array arrayValue:
                writer.WriteStartArray();
                foreach (object? item in arrayValue)
                {
                    WriteValue(writer, item, options);
                }
                writer.WriteEndArray();
                break;

            case Dictionary<string, object> dictValue:
                Write(writer, dictValue, options);
                break;

            case null:
                writer.WriteNullValue();
                break;

            default:
                JsonSerializer.Serialize(writer, value, options);
                break;
        }
    }
}