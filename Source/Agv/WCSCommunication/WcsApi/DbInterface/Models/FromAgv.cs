namespace Gebhardt.Acc.WCSCommunication.WcsApi.DbInterface.Models;

/// <summary>
/// Write status to WCS
/// </summary>
public class FromAgv : ToFromAgvBase
{
    /// <summary>
    ///  Factory method to creates new FromAgv entry based on the provided ToAgv instance
    /// <para>
    /// The MessageStatus is automatically set to <see cref="MessageStatus.Pending"/>
    /// The CcuVersion is incremented by 1
    /// </para></summary>
    /// <param name="order">Order the new FromAgv instance is based on</param>
    /// <param name="status">Status of this new FromAgv instance</param>
    /// <param name="recordType">RecordType of this new FromAgv instance</param>
    /// <param name="vehicle">Assigned AGV</param>
    /// <param name="error">Error Code</param>
    /// <returns>FromAgv instance</returns>
    public static FromAgv CreateInstance(ToAgv order, OrderStatus status, RecordType recordType, string? vehicle = null, string? error = null)
    {
        return new FromAgv
        {
            MessageStatus = MessageStatus.Pending,
            Status = status,
            RecordType = recordType,
            CcuVersion = order.CcuVersion + 1,
            Vehicle = vehicle ?? order.Vehicle,
            Error = error,
            // Copied members
            OrderId = order.OrderId,
            OrderType = order.OrderType,
            LeNo = order.LeNo,
            Source = order.Source,
            Destination = order.Destination,
            Creator = order.Creator,
            Created = order.Created
        };
    }
}