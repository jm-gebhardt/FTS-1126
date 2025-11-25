using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Domain.Profiles;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Profiles;

[TestClass]
public class AgvWifiMetricsProfileTest
{
    [TestMethod]
    public void Map_ValidData_MapsAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new AgvWifiMetricsProfile());

        var wifiMetrics = WifiMetricsGenerator.GenerateWifiMetrics();
        var result = mapper.Map<Domain.Models.AgvWifiMetrics>(wifiMetrics);
        result.ShouldNotBeNull();
        result.Ssid.ShouldBe(wifiMetrics.Ssid);
        result.Band.ShouldBe(wifiMetrics.Band);
        result.Channel.ShouldBe(wifiMetrics.Channel);
        result.Frequency.ShouldBe(wifiMetrics.Frequency);
        result.BeaconSignalAverage.ShouldBe(wifiMetrics.BeaconSignalAverage);
        result.Quality.ShouldBe(wifiMetrics.Quality);
        result.Noise.ShouldBe(wifiMetrics.Noise);
        result.Timestamp.ShouldBe(wifiMetrics.Timestamp);
        result.Id.ShouldNotBe(Guid.Empty);
        result.GridCellId.ShouldBe(Guid.Empty);
        result.GridCell.ShouldBeNull();
    }
}