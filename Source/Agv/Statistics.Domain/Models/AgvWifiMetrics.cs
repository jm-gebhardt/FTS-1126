namespace Gebhardt.Acc.Statistics.Domain.Models;

/// Represents the Wi-Fi metrics data for an AGV, including position, signal strength, and quality.
public class AgvWifiMetrics : IPositionedEntity
{
    /// <summary>
    /// Unique identifier for the AGV Wi-Fi metrics entity.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The PositionId of the Agv grid position.
    /// </summary>
    public Guid PositionId { get; set; }

    /// <inheritdoc />
    public GridAgvPosition? Position { get; init; }

    /// <inheritdoc />
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// SSID of the wireless network.
    /// </summary>
    public required string Ssid { get; init; }

    /// <summary>
    /// Wireless band or modulation type (e.g., 2.4GHz, 5GHz).
    /// </summary>
    public string? Band { get; init; }

    /// <summary>
    /// Channel index used for wireless communication.  
    /// A value of -1 may indicate a scanning or undefined state.
    /// </summary>
    public int? Channel { get; init; }

    /// <summary>
    /// Operating frequency of the wireless interface [MHz].  
    /// A value of 0 may indicate a scanning or undefined state.
    /// </summary>
    public int? Frequency { get; init; }

    /// <summary>
    /// Average signal strength of recently received beacons [dBm].
    /// </summary>
    public int? BeaconSignalAverage { get; init; }

    /// <summary>
    /// Quality of the wireless link [0–100 or device-specific scale].
    /// </summary>
    public int? Quality { get; init; }

    /// <summary>
    /// Detected noise level on the radio channel [dBm].
    /// </summary>
    public int? Noise { get; init; }

    /// <inheritdoc cref="AgvError"/>>.
    public AgvWifiMetrics()
    {
    }
}