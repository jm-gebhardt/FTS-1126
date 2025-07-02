using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Common.Testing.Helpers;

public static class WifiMetricsGenerator
{
    public static WifiMetrics GenerateWifiMetrics(
        string ssid = "ssid",
        string? band = "2.4GHz",
        int? channel = 1,
        int? frequency = 2412,
        int? beaconSignalAverage = -70,
        int? quality = 80,
        int? noise = -90,
        DateTime timeStamp = default
    )
    {
        if (timeStamp == default)
        {
            timeStamp = DateTime.UtcNow;
        }

        return new WifiMetrics(ssid, band, channel, frequency, beaconSignalAverage, quality, noise, timeStamp);
    }
}