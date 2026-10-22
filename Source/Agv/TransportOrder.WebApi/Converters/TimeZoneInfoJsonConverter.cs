using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.TransportOrder.WebApi.Converters;

/// <summary>
/// Writes a <see cref="TimeZoneInfo"/> in the shape of the default serialization, but with an IANA id.
/// On Windows <see cref="TimeZoneInfo.Id"/> is a Windows id, which browsers and the KITT frontend cannot resolve.
/// </summary>
public sealed class TimeZoneInfoJsonConverter : JsonConverter<TimeZoneInfo>
{
    /// <inheritdoc />
    public override TimeZoneInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException($"{nameof(TimeZoneInfo)} is only written by {nameof(TimeZoneInfoJsonConverter)}.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeZoneInfo value, JsonSerializerOptions options)
    {
        string id = GetIanaId(value, out bool hasIanaId);

        writer.WriteStartObject();
        writer.WriteString("id", id);
        writer.WriteBoolean("hasIanaId", hasIanaId);
        writer.WriteString("displayName", value.DisplayName);
        writer.WriteString("standardName", value.StandardName);
        writer.WriteString("daylightName", value.DaylightName);
        writer.WriteString("baseUtcOffset", value.BaseUtcOffset.ToString("c", CultureInfo.InvariantCulture));
        writer.WriteBoolean("supportsDaylightSavingTime", value.SupportsDaylightSavingTime);
        writer.WriteEndObject();
    }

    private static string GetIanaId(TimeZoneInfo timeZone, out bool hasIanaId)
    {
        hasIanaId = true;

        if (timeZone.HasIanaId)
        {
            return timeZone.Id;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out string? ianaId))
        {
            return ianaId;
        }

        hasIanaId = false;
        return timeZone.Id;
    }
}
