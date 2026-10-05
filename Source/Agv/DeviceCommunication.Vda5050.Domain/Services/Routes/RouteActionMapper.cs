using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using RouteAgvAction = Gebhardt.Acc.Common.Messages.Route.AgvActions.AgvAction;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;

/// <summary>
/// Maps a <see cref="RouteAgvAction"/> named by a <see cref="Common.Messages.Route.RouteSteps.RouteStep"/> onto a
/// VDA5050 <see cref="Models.Orders.OrderAction"/>, enriching the parameters TransportOrder supplied with
/// fact-sheet-derived ones (blocking behavior, min/max heights, etc.), by invoking the existing, action-type
/// specific <see cref="IOrderActionBuilder"/> directly.
/// </summary>
public class RouteActionMapper(
    IAgvActionRepository agvActionRepository,
    PickOrderActionBuilder pickOrderActionBuilder,
    DropOrderActionBuilder dropOrderActionBuilder,
    StartChargingOrderActionBuilder startChargingOrderActionBuilder)
{
    private const string PickHeightParameterKey = "pickHeight";
    private const string DropHeightParameterKey = "dropHeight";

    /// <summary>
    /// Map the given route action onto a VDA5050 order action for the given node, or null if there is no action
    /// to perform there.
    /// </summary>
    /// <param name="agv">The agv the order is built for.</param>
    /// <param name="graphNode">The graph node the action is to be performed at.</param>
    /// <param name="routeAction">The route action, or null.</param>
    /// <returns>The order action, or null if <paramref name="routeAction"/> is null.</returns>
    /// <exception cref="RouteActionNotSupportedByAgvException">The agv's fact sheet does not declare support for the resolved action type.</exception>
    public async Task<OrderAction?> MapAsync(Agv agv, Node graphNode, RouteAgvAction? routeAction)
    {
        if (routeAction == null)
        {
            return null;
        }

        string actionType = ResolveActionType(routeAction);
        var factSheetProperty = graphNode.GetPropertiesForVehicle(agv.VehicleTypeId)?.Actions?.SingleOrDefault(a => a.ActionType == actionType);

        var agvActions = await agvActionRepository.GetActionsForScopeAsync(agv.SerialNumber, Models.AgvActions.ActionScope.Node);
        var definedAction = agvActions.SingleOrDefault(a => a.ActionType == actionType) ?? throw new RouteActionNotSupportedByAgvException(actionType, agv.CorrelationId);

        var parameters = factSheetProperty?.ActionParameters?.Select(p => new OrderActionParameter { Key = p.Key, Value = p.Value }).ToList() ?? [];
        AddRouteActionParameters(parameters, routeAction);

        var context = new OrderActionContext(parameters, factSheetProperty?.BlockingType, true);
        return BuildOrderAction(actionType, definedAction, context);
    }

    private static string ResolveActionType(RouteAgvAction routeAction)
    {
        return routeAction switch
        {
            Common.Messages.Route.AgvActions.PickAction => Vda5050Specifications.ActionTypes.Pick,
            Common.Messages.Route.AgvActions.DropAction => Vda5050Specifications.ActionTypes.Drop,
            Common.Messages.Route.AgvActions.ChargeAction => Vda5050Specifications.ActionTypes.StartCharging,
            _ => throw new NotSupportedException($"Unsupported route action type '{routeAction.GetType().Name}'")
        };
    }

    private static void AddRouteActionParameters(List<OrderActionParameter> parameters, RouteAgvAction routeAction)
    {
        switch (routeAction)
        {
            case Common.Messages.Route.AgvActions.PickAction { PickHeight: { } pickHeight }:
                parameters.Add(new OrderActionParameter { Key = PickHeightParameterKey, Value = pickHeight });
                break;

            case Common.Messages.Route.AgvActions.DropAction { DropHeight: { } dropHeight }:
                parameters.Add(new OrderActionParameter { Key = DropHeightParameterKey, Value = dropHeight });
                break;
        }
    }

    private OrderAction BuildOrderAction(string actionType, Models.AgvActions.AgvAction definedAction, OrderActionContext context)
    {
        if (actionType == Vda5050Specifications.ActionTypes.Pick)
        {
            return pickOrderActionBuilder.Build(definedAction, context);
        }
        if (actionType == Vda5050Specifications.ActionTypes.Drop)
        {
            return dropOrderActionBuilder.Build(definedAction, context);
        }
        return startChargingOrderActionBuilder.Build(definedAction, context);
    }
}
