using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;

/// <summary>
/// Provides a builder for the predefined VDA5050 order action of type 'finePositioning'.
/// </summary>
public class FinePositioningOrderActionBuilder : IOrderActionBuilder
{
    /// <inheritdoc/>
    public string ActionType => Vda5050Specifications.ActionTypes.FinePositioning;

    /// <inheritdoc/>
    public Models.Orders.OrderAction Build(AgvAction agvAction, OrderActionContext context)
    {
        var parameters = new List<OrderActionParameter>();
        parameters.AddIfRequired(agvAction, Vda5050Specifications.Parameters.StationName, context.Vda5050Parameters.StationName!);
        parameters.AddIfRequired(agvAction, Vda5050Specifications.Parameters.StationType, context.Vda5050Parameters.StationType!);

        return new Models.Orders.OrderAction(ActionType, agvAction.ActionDescription, agvAction.ResolveBlockingType(context.BlockingType), parameters);
    }
}