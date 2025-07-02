using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents WiFi-related metrics.
/// </summary>
/// <param name="Ssid">SSID of the wireless network.</param>
/// <param name="Band">Wireless band or modulation type (e.g., 2.4GHz, 5GHz).</param>
/// <param name="Channel">Channel index used for wireless communication. A value of -1 may indicate a scanning or undefined state. </param>
/// <param name="Frequency">Operating frequency of the wireless interface [MHz]. A value of 0 may indicate a scanning or undefined state. </param>
/// <param name="BeaconSignalAverage">Average signal strength of recently received beacons [dBm].</param>
/// <param name="Noise">Detected noise level on the radio channel [dBm].</param>
/// <param name="Quality">Quality of the wireless link [0–100 or device-specific scale].</param>
/// <param name="Timestamp">The time when the metrics were recorded.</param>
[UsedImplicitly]
public record WifiMetrics(
    string Ssid,
    string? Band,
    int? Channel,
    int? Frequency,
    int? BeaconSignalAverage,
    int? Quality,
    int? Noise,
    DateTime Timestamp
);