using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Extension methods for <see cref="AgvAction"/>
/// </summary>
public static class AgvActionExtensions
{
    private static DateTime GetCurrentDate() => TimeProvider.Now;

    /// <summary>
    /// Updates the state of the Action and its Dates
    /// </summary>
    /// <param name="action"></param>
    /// <param name="state"></param>
    public static void UpdateActionState(this AgvAction action, ActionState state)
    {
        var previousState = action.State;
        action.State = state;

        SetDates(action, previousState);
    }

    private static void SetDates(AgvAction action, ActionState previousState)
    {
        var state = action.State;
        if (previousState != state)
        {
            switch (state)
            {
                case ActionState.Started:
                    action.StartedDate = GetCurrentDate();
                    break;

                case ActionState.Completed:
                case ActionState.Failed:
                case ActionState.Canceled:
                    action.CompletedDate = GetCurrentDate();
                    break;
            }
        }
    }
}