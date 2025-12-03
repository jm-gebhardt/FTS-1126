using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace Gebhardt.Acc.WCSCommunication.WcsApi.DbInterface.Models;

/// <summary>
/// Order Status
/// </summary>
[PublicAPI]
public enum OrderStatus
{
    /// <summary>
    /// New order, expected type 
    /// </summary>
    Initial,

    /// <summary>
    /// Order has been consumed but not executed
    /// </summary>
    Pending,

    /// <summary>
    /// Order execution has started
    /// </summary>
    InProgress,

    /// <summary>
    /// Order completed
    /// </summary>
    Finished,

    /// <summary>
    /// Order canceled
    /// </summary>
    Canceled
}

/// <summary>
/// Message Status defining if the DB entry has been consumed by WCSCommunication or WCS
/// </summary>
[PublicAPI]
public enum MessageStatus
{
    /// <summary>
    /// DB entry has not been read
    /// </summary>
    Pending,

    /// <summary>
    /// DB entry has been read
    /// </summary>
    Finished,

    /// <summary>
    /// DB entry could not be consumed
    /// </summary>
    Failed
}

/// <summary>
/// RecordType describes an order action
/// </summary>
[PublicAPI]
public enum RecordType
{
    /// <summary>
    /// Initialize a specific AGV.
    /// This is a deprecated, legacy field and no longer used.
    /// </summary>
    [Obsolete("Initialize is not longer handled by WCS", true)]
    Initialize,

    /// <summary>
    /// New order
    /// </summary>
    TransportOrder,

    /// <summary>
    /// Update existing order
    /// </summary>
    TransportOrderUpdate,

    /// <summary>
    /// Cancel existing order
    /// </summary>
    OrderCancelRequest,

    /// <summary>
    /// Acknowledge order cancellation
    /// </summary>
    OrderCancelAcknowledge,

    /// <summary>
    /// Order cancellation has failed
    /// </summary>
    OrderCancelFailed
}

/// <summary>
/// Nature of this order
/// </summary>
[PublicAPI]
public enum OrderType
{
    /// <summary>
    /// This order is a transport-order
    /// </summary>
    Transport,

    /// <summary>
    /// Deprecated
    /// </summary>
    [Obsolete("ClearLam is not longer handled by WCS", true)]
    ClearLam,

    /// <summary>
    /// Deprecated
    /// </summary>
    [Obsolete("Initialize is not longer handled by WCS", true)]
    Initialize
}

/// <summary>
/// DB Scheme for ToAgv and FromAgv
/// </summary>
[PublicAPI]
public abstract class ToFromAgvBase
{
    /// <summary>
    /// Auto-increment id
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; init; }

    /// <summary>
    ///  Action described in this entry
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public RecordType RecordType { get; init; }

    /// <summary>
    /// Message Status
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public MessageStatus MessageStatus { get; set; }

    /// <summary>
    /// Order Id
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public string OrderId { get; init; } = null!;

    /// <summary>
    /// Order 
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public OrderType OrderType { get; init; }

    /// <summary>
    /// The LeNo of the orders handled box or transportation good
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public string LeNo { get; init; } = null!;

    /// <summary>
    /// The source where to pick up the order
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public string Source { get; init; } = null!;

    /// <summary>
    /// The destination where to drop the order
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public string Destination { get; init; } = null!;

    /// <summary>
    /// Assigned AGV
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public string? Vehicle { get; init; }

    /// <summary>
    /// Current order status
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public OrderStatus Status { get; init; }

    /// <summary>
    /// Order error
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public string? Error { get; init; }

    /// <summary>
    /// Creator of this entry
    /// </summary>
    [Column(TypeName = "nvarchar(255)")]
    public string? Creator { get; init; }

    /// <summary>
    /// UTC timestamp of first order creation
    /// </summary>
    public DateTime Created { get; init; }

    /// <summary>
    /// Number of Updates
    /// </summary>
    public int CcuVersion { get; init; }

    /// <summary>
    /// UTC timestamp of last update
    /// </summary>
    public DateTime TimeStamp { get; init; } = DateTime.UtcNow;
}