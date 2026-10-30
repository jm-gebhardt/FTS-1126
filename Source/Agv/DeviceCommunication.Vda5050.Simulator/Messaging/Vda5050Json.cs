using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeviceCommunication.Vda5050.Simulator.Messaging;

/// <summary>
/// JSON options for the VDA5050 wire format: camelCase properties, upper-case enum values and ISO8601 timestamps.
/// </summary>
public static class Vda5050Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(new UpperCaseNamingPolicy()),
            new TimestampConverter()
        }
    };

    private sealed class UpperCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToUpperInvariant();
    }

    private sealed class TimestampConverter : JsonConverter<DateTimeOffset>
    {
        private const string Format = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTimeOffset();

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
    }
}
