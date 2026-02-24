using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages;

/// <summary>
/// Defines the creator of a host order 
/// </summary>
[PublicAPI]
public enum HostOrderCreator
{
    /// <summary>
    /// A Warehouse Management System created the order (e.g. StoreWare)
    /// </summary>
    Wms,

    /// <summary>
    /// A simulation module is creator of the order
    /// </summary>
    Simulation,

    /// <summary>
    /// The TestPlanRunner service created the order
    /// </summary>
    TestPlanRunner
}