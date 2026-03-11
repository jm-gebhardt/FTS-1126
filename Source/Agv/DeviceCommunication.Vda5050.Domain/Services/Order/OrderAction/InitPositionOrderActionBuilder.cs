using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;

/// <summary>
/// Provides a builder for the predefined VDA5050 order action of type 'initPosition'.
/// </summary>
public sealed class InitPositionOrderActionBuilder : IOrderActionBuilder
{
    /// <inheritdoc/>
    public string ActionType => Vda5050Specifications.ActionTypes.InitPosition;

    /// <inheritdoc/>
    public Models.Orders.OrderAction Build(AgvAction agvAction, OrderActionContext context)
    {
        Validate(agvAction, context);

        var parameters = new List<OrderActionParameter>
        {
            new() { Key = Vda5050Specifications.Parameters.X, Value = context.Vda5050Parameters.Position!.X },
            new() { Key = Vda5050Specifications.Parameters.Y, Value = context.Vda5050Parameters.Position!.Y },
            new() { Key = Vda5050Specifications.Parameters.Theta, Value = context.Vda5050Parameters.Position!.Theta },
            new() { Key = Vda5050Specifications.Parameters.LastNodeId, Value = context.Vda5050Parameters.LastNodeId!},
            new() { Key = Vda5050Specifications.Parameters.MapId, Value = context.Vda5050Parameters.MapId!}
        };

        return new Models.Orders.OrderAction(ActionType, agvAction.ActionDescription, agvAction.ResolveBlockingType(context.BlockingType), parameters);
    }

    private void Validate(AgvAction agvAction, OrderActionContext context)
    {
        if (agvAction.ActionParameters == null || agvAction.ActionParameters.Count == 0)
        {
            throw new RequiredAgvActionParameterMissing(ActionType);
        }

        if (context.Vda5050Parameters.Position == null)
        {
            throw new RequiredOrderActionParameterMissing(Vda5050Specifications.Parameters.X, ActionType);
        }

        if (context.Vda5050Parameters.LastNodeId == null)
        {
            throw new RequiredOrderActionParameterMissing(Vda5050Specifications.Parameters.LastNodeId, ActionType);
        }

        if (context.Vda5050Parameters.MapId == null)
        {
            throw new RequiredOrderActionParameterMissing(Vda5050Specifications.Parameters.MapId, ActionType);
        }
    }
}