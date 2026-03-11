using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;

/// <summary>
/// Provides a builder for the predefined VDA5050 order action of type 'startCharging'.
/// </summary>
public class StartChargingOrderActionBuilder : IOrderActionBuilder
{
    /// <inheritdoc/>
    public string ActionType => Vda5050Specifications.ActionTypes.StartCharging;

    /// <inheritdoc/>
    public Models.Orders.OrderAction Build(AgvAction agvAction, OrderActionContext context)
    {
        return new Models.Orders.OrderAction(ActionType, agvAction.ActionDescription, agvAction.ResolveBlockingType(context.BlockingType), []);
    }
}