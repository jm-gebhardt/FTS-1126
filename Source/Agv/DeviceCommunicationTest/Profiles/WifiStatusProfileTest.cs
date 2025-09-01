using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Profiles;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Profiles;

[TestClass]
public class WifiStatusProfileTest
{
    private Mapper _mapper = null!;

    [TestInitialize]
    public void SetUp()
    {
        _mapper = MappingHelper.GetMapper(new WifiStatusProfile());
    }

    [TestMethod]
    public void Map_ShouldMapMqttWifiStatusToWifiMetricsCorrectly()
    {
        var mqttWifiStatus = WifiStatusGenerator.GenerateWifiStatus(
            ssid: "TestSSID",
            band: "5GHz",
            channel: 36,
            frequency: 5180,
            beaconSignalAverage: -50,
            quality: 75,
            noise: -90
        );
        var timestamp = DateTime.UtcNow;

        var wifiMetrics = _mapper.Map<WifiMetrics>(mqttWifiStatus, opts => { opts.Items[nameof(WifiMetrics.Timestamp)] = timestamp; });

        wifiMetrics.ShouldSatisfyAllConditions(
            p => p.Ssid.ShouldBe(mqttWifiStatus.Ssid),
            p => p.Band.ShouldBe(mqttWifiStatus.Band),
            p => p.Channel.ShouldBe(mqttWifiStatus.Channel),
            p => p.Frequency.ShouldBe(mqttWifiStatus.Frequency),
            p => p.BeaconSignalAverage.ShouldBe(mqttWifiStatus.BeaconSignalAverage),
            p => p.Noise.ShouldBe(mqttWifiStatus.Noise),
            p => p.Quality.ShouldBe(mqttWifiStatus.Quality),
            p => p.Timestamp.ShouldBe(timestamp)
        );
    }

    [TestMethod]
    public void Map_ShouldMapZeroValuesToNull()
    {
        var mqttWifiStatus = WifiStatusGenerator.GenerateWifiStatus(
            channel: 0,
            frequency: 0,
            beaconSignalAverage: 0,
            noise: 0
        );

        var wifiMetrics = _mapper.Map<WifiMetrics>(mqttWifiStatus, opts => { opts.Items[nameof(WifiMetrics.Timestamp)] = default(DateTime); });

        wifiMetrics.ShouldSatisfyAllConditions(
            p => p.Channel.ShouldBeNull(),
            p => p.Frequency.ShouldBeNull(),
            p => p.BeaconSignalAverage.ShouldBeNull(),
            p => p.Noise.ShouldBeNull()
        );
    }

    [TestMethod]
    public void Map_ShouldMapEmptyBandToNull()
    {
        var mqttWifiStatus = WifiStatusGenerator.GenerateWifiStatus(band: string.Empty);

        var wifiMetrics = _mapper.Map<WifiMetrics>(mqttWifiStatus, opts => { opts.Items[nameof(WifiMetrics.Timestamp)] = default(DateTime); });

        wifiMetrics.Band.ShouldBeNull();
    }
}