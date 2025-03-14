namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;

/// <summary>
/// Represents an agv
/// </summary>
public class Agv
{
    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The serial number of the agv.
    /// </summary>
    public string SerialNumber { get; init; }

    /// <inheritdoc cref="Agv"/>>
    public Agv (Guid correlationId, string serialNumber)
    {
        CorrelationId=correlationId;
        SerialNumber=serialNumber;
    }
}