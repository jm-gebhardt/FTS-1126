using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;

/// <summary>
/// Provides an interface for order action builders.
/// </summary>
public interface IOrderActionBuilder
{
    /// <summary>
    /// The action type (pick, startCharging etc.).
    /// </summary>
    string ActionType { get; }

    /// <summary>
    /// Build an order action for this agv action and context.
    /// </summary>
    /// <param name="agvAction">The agv action.</param>
    /// <param name="context">The build context.</param>
    /// <returns>An order action.</returns>
    Models.Orders.OrderAction Build(AgvAction agvAction, OrderActionContext context);
}