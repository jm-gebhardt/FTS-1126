using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Agv State
/// </summary>
public enum AgvState
{
    /// <summary>
    /// Initial
    /// </summary>
    [UsedImplicitly]
    Initial,

    /// <summary>
    /// Discovered
    /// </summary>
    [UsedImplicitly]
    Discovered,

    /// <summary>
    /// Idle
    /// </summary>
    [UsedImplicitly]
    Idle,

    /// <summary>
    /// Busy
    /// </summary>
    [UsedImplicitly]
    Busy,

    /// <summary>
    /// Charging
    /// </summary>
    [UsedImplicitly]
    Charging
};