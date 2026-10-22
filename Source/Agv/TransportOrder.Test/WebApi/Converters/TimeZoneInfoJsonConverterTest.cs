using System.Text.Json;
using Gebhardt.Acc.TransportOrder.WebApi.Converters;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Converters;

[TestClass]
public class TimeZoneInfoJsonConverterTest
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new TimeZoneInfoJsonConverter() }
    };

    [TestMethod]
    public void Serialize_WritesIanaId_RegardlessOfPlatformId()
    {
        // Europe/Berlin resolves to a Windows id on Windows and stays IANA on Linux.
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(timeZone, _options));

        document.RootElement.GetProperty("id").GetString().ShouldBe("Europe/Berlin");
        document.RootElement.GetProperty("hasIanaId").GetBoolean().ShouldBeTrue();
    }

    [TestMethod]
    public void Serialize_WritesTheSamePropertiesAsTheDefaultSerialization()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(timeZone, _options));

        document.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(
            ["id", "hasIanaId", "displayName", "standardName", "daylightName", "baseUtcOffset", "supportsDaylightSavingTime"]);
        document.RootElement.GetProperty("baseUtcOffset").GetString().ShouldBe(timeZone.BaseUtcOffset.ToString("c"));
        document.RootElement.GetProperty("supportsDaylightSavingTime").GetBoolean().ShouldBe(timeZone.SupportsDaylightSavingTime);
    }

    [TestMethod]
    public void Serialize_KeepsTheId_WhenItCannotBeConverted()
    {
        var timeZone = TimeZoneInfo.CreateCustomTimeZone("Custom/Zone", TimeSpan.FromHours(2), "Custom", "Custom");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(timeZone, _options));

        document.RootElement.GetProperty("id").GetString().ShouldBe("Custom/Zone");
    }
}
