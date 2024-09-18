using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;

/// <summary>
/// The order record type
/// </summary>
[PublicAPI]
public enum OrderRecordType
{
    /// <summary>
    /// Normal/new order
    /// </summary>
    Order,

    /// <summary>
    /// Update an order
    /// </summary>
    OrderUpdate,

    /// <summary>
    /// Cancel an order
    /// </summary>
    OrderCancel
}