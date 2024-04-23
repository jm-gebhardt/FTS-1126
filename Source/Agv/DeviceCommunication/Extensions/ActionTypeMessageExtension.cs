using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.DeviceCommunication.Messages.Enums;

namespace Gebhardt.Acc.DeviceCommunication.Extensions;

/// <summary>
/// Provides extension method for <see cref="ActionType"/>
/// to convert to types used in messages sent to the agv.
/// </summary>
public static class ActionTypeMessageExtension
{
    /// <summary>
    /// Converts an <see cref="ActionType"/> to the corresponding
    /// <inheritdoc cref="OrderType"/>.
    /// </summary>
    /// <param name="actionType">The action type.</param>
    /// <returns>The order type.</returns>
    public static OrderType ToOrderType(this ActionType actionType)
    {
        return actionType switch
        {
            ActionType.Pick => OrderType.Pick,
            ActionType.Drop => OrderType.Drop,
            ActionType.Move => OrderType.Move,
            ActionType.Charge => OrderType.Charge,
            _ => throw new InvalidOperationException($"Can't handle {actionType} yet")
        };
    }
}