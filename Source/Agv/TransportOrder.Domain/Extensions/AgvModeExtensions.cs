using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.Domain.Extensions;

/// <summary>
/// Extension methods for AgvMode.
/// </summary>
public static class AgvModeExtensions
{
    /// <summary>
    /// Returns the pause reason depending on AGV mode.
    /// </summary>
    /// <param name="agvMode"></param>
    public static PauseReason GetPauseReason(this AgvMode agvMode)
    {
        ModeType? modeToCheck = agvMode.PendingType ?? agvMode.Type;
        var pauseReason = modeToCheck switch
        {
            ModeType.UserMode => PauseReason.UserMode,
            ModeType.Locked => PauseReason.LockMode,
            ModeType.Maintenance => PauseReason.MaintenanceMode,
            _ => PauseReason.None
        };
        return pauseReason;
    }
}