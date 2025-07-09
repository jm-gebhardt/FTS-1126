using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Services.Filters;

[TestClass]
public class WifiMetricsSignificanceCheckerTest
{
    private WifiConfiguration _config = null!;
    private WifiMetricsSignificanceChecker _checker = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new WifiConfiguration(
            BeaconSignalAverageThreshold: 5,
            QualityThreshold: 10,
            NoiseThreshold: 3);
        _checker = new WifiMetricsSignificanceChecker(_config);
    }

    [TestMethod]
    public void HasSignificantChanges_TimestampNotConsideredSignificant()
    {
        var now = DateTime.UtcNow;
        var baseMetrics = WifiMetricsGenerator.GenerateWifiMetrics(timeStamp: now);

        var modified = baseMetrics with { Timestamp = now.AddHours(1) };

        _checker.HasSignificantChanges(modified, baseMetrics).ShouldBeFalse();
    }

    [DataTestMethod]
    [DataRow(nameof(WifiMetrics.BeaconSignalAverage), 5, true)]
    [DataRow(nameof(WifiMetrics.BeaconSignalAverage), -5, true)]
    [DataRow(nameof(WifiMetrics.BeaconSignalAverage), 4, false)]
    [DataRow(nameof(WifiMetrics.Quality), 10, true)]
    [DataRow(nameof(WifiMetrics.Quality), -10, true)]
    [DataRow(nameof(WifiMetrics.Quality), 9, false)]
    [DataRow(nameof(WifiMetrics.Noise), 3, true)]
    [DataRow(nameof(WifiMetrics.Noise), -3, true)]
    [DataRow(nameof(WifiMetrics.Noise), 2, false)]
    public void HasSignificantChanges_MetricThresholdAtBoundary_ReturnsExpected(string propertyName, int difference, bool shouldDetectChange)
    {
        var baseMetrics = WifiMetricsGenerator.GenerateWifiMetrics(
            beaconSignalAverage: 0,
            quality: 0,
            noise: 0
        );

        var modified = propertyName switch
        {
            nameof(WifiMetrics.BeaconSignalAverage) => baseMetrics with { BeaconSignalAverage = difference },
            nameof(WifiMetrics.Quality) => baseMetrics with { Quality = difference },
            nameof(WifiMetrics.Noise) => baseMetrics with { Noise = difference },
            _ => throw new ArgumentException($"Unknown metric: {propertyName}")
        };

        _checker.HasSignificantChanges(modified, baseMetrics).ShouldBe(shouldDetectChange);
    }

    [TestMethod]
    public void HasSignificantChanges_BeaconSignalAverageIsNullVsValue_ReturnsTrue()
    {
        var baseMetrics = WifiMetricsGenerator.GenerateWifiMetrics(beaconSignalAverage: null);

        var changed = baseMetrics with { BeaconSignalAverage = 0 };

        _checker.HasSignificantChanges(changed, baseMetrics).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_QualityIsNullVsValue_ReturnsTrue()
    {
        var baseMetrics = WifiMetricsGenerator.GenerateWifiMetrics(quality: null);

        var changed = baseMetrics with { Quality = 0 };

        _checker.HasSignificantChanges(changed, baseMetrics).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_NoiseIsNullVsValue_ReturnsTrue()
    {
        var baseMetrics = WifiMetricsGenerator.GenerateWifiMetrics(noise: null);

        var changed = baseMetrics with { Noise = 0 };

        _checker.HasSignificantChanges(changed, baseMetrics).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_SsidChanged_ReturnsTrue()
    {
        var original = WifiMetricsGenerator.GenerateWifiMetrics(ssid: "OldSSID");

        var modified = original with { Ssid = "NewSSID" };

        _checker.HasSignificantChanges(modified, original).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_AllPropertiesIdentical_ReturnsFalse()
    {
        var metrics = WifiMetricsGenerator.GenerateWifiMetrics();

        _checker.HasSignificantChanges(metrics, metrics).ShouldBeFalse();
    }
}