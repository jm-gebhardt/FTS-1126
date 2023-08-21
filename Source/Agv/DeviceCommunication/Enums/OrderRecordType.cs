using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;

/// <summary>
/// The record type of an order to the AGV
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