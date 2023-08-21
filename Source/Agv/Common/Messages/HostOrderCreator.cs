namespace Gebhardt.StoreWare.Agv.Common.Messages;

/// <summary>
/// Defines the creator of a host order 
/// </summary>
[JetBrains.Annotations.PublicAPI]
public enum HostOrderCreator
{
    /// <summary>
    /// A Warehouse Management System created the order (e.g. StoreWare)
    /// </summary>
    Wms,
    /// <summary>
    /// A simulation module is creator of the order
    /// </summary>
    Simulation
}