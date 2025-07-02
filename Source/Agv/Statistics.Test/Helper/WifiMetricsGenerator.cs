using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Test.Helper;

public static class AgvWifiMetricsGenerator
{
    public static AgvWifiMetrics GenerateWifiMetrics(
        Guid positionId,
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

        return new AgvWifiMetrics
        {
            PositionId = positionId,
            Ssid = ssid,
            Band = band,
            Channel = channel,
            Frequency = frequency,
            BeaconSignalAverage = beaconSignalAverage,
            Quality = quality,
            Noise = noise,
            Timestamp = timeStamp
        };
    }
}