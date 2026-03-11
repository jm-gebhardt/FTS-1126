using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;

/// <summary>
/// Provides a builder for the predefined VDA5050 order action of type 'pick'.
/// </summary>
public sealed class PickOrderActionBuilder : IOrderActionBuilder
{
    /// <inheritdoc/>
    public string ActionType => Vda5050Specifications.ActionTypes.Pick;

    /// <inheritdoc/>
    public Models.Orders.OrderAction Build(AgvAction agvAction, OrderActionContext context)
    {
        Validate(agvAction, context);

        var parameters = new List<OrderActionParameter>
        {
            new() { Key = Vda5050Specifications.Parameters.StationType, Value = context.Vda5050Parameters.StationType! }
        };

        AddOptionalParameters(parameters, agvAction, context);

        return new Models.Orders.OrderAction(ActionType, agvAction.ActionDescription, agvAction.ResolveBlockingType(context.BlockingType), parameters);
    }

    private void Validate(AgvAction agvAction, OrderActionContext context)
    {
        if (agvAction.ActionParameters == null || agvAction.ActionParameters.Count == 0)
        {
            throw new RequiredAgvActionParameterMissing(ActionType);
        }

        if (context.Vda5050Parameters.StationType == null)
        {
            throw new RequiredOrderActionParameterMissing(Vda5050Specifications.Parameters.StationType, ActionType);
        }

        if (context.Vda5050Parameters.StationName == null && agvAction.IsParameterRequired(Vda5050Specifications.Parameters.StationName))
        {
            throw new RequiredOrderActionParameterMissing(Vda5050Specifications.Parameters.StationName, ActionType);
        }

        if (context.Vda5050Parameters.LoadHandlingDevice == null && agvAction.IsParameterRequired(Vda5050Specifications.Parameters.LoadHandlingDevice))
        {
            throw new RequiredOrderActionParameterMissing(Vda5050Specifications.Parameters.StationName, ActionType);
        }
    }

    private void AddOptionalParameters(List<OrderActionParameter> parameters, AgvAction agvAction, OrderActionContext context)
    {
        parameters.AddIfRequired(agvAction, Vda5050Specifications.Parameters.StationName, context.Vda5050Parameters.StationName!);
        parameters.AddIfRequired(agvAction, Vda5050Specifications.Parameters.LoadHandlingDevice, context.Vda5050Parameters.LoadHandlingDevice!);
    }
}