using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunicationTest.Helper;

public static class WifiStatusGenerator
{
    public static MqttWifiStatus GenerateWifiStatus(
        string ssid = "ssid",
        string? band = "2.4GHz",
        int channel = 1,
        int frequency = 2412,
        int beaconSignalAverage = -70,
        int quality = 80,
        int noise = -90
    )
    {
        return new MqttWifiStatus
        {
            Ssid = ssid,
            Band = band,
            Channel = channel,
            Frequency = frequency,
            BeaconSignalAverage = beaconSignalAverage,
            Quality = quality,
            Noise = noise
        };
    }
}