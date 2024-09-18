using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv State Dto
/// </summary>
[PublicAPI]
public enum AgvStateType
{
    /// <summary>
    /// Initial
    /// </summary>
    Initial,

    /// <summary>
    /// Discovered
    /// </summary>
    Discovered,

    /// <summary>
    /// Idle
    /// </summary>
    Idle,

    /// <summary>
    /// Busy
    /// </summary>
    Busy
}