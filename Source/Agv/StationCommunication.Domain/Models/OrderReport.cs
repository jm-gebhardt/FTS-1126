namespace Gebhardt.Acc.StationCommunication.Domain.Models;

/// <summary>
/// A report of an order to a station.
/// </summary>
public class OrderReport
{
    /// <summary>
    /// The id of the host order.
    /// </summary>
    public Guid HostOrderId { get; init; }

    /// <summary>
    /// The station the order was reported to.
    /// </summary>
    public string Station { get; init; }

    /// <inheritdoc cref="OrderReport"/>>
    public OrderReport(Guid hostOrderId, string station)
    {
        HostOrderId = hostOrderId;
        Station = station;
    }
}