namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// The agv position
/// </summary>
public record Vda5050Position
{
    /// <summary>
    /// X-coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// X-coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// X-coordinate.
    /// </summary>
    public float Theta { get; set; }
}