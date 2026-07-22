using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;

/// <summary>
/// Provides a registry for order action building.
/// </summary>
public sealed class OrderActionBuilderRegistry
{
    private readonly IReadOnlyDictionary<string, IOrderActionBuilder> _builders;

    /// <inheritdoc cref="OrderActionBuilderRegistry"/>
    public OrderActionBuilderRegistry(IEnumerable<IOrderActionBuilder> builders)
        => _builders = builders.ToDictionary(
            b => b.ActionType,
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Build the order action using factsheet information and a build context.
    /// </summary>
    /// <param name="agvAction">The agv action.</param>
    /// <param name="context">The build context.</param>
    /// <returns>The order action.</returns>
    public Models.Orders.OrderAction Build(AgvAction agvAction, OrderActionContext context)
    {
        if (_builders.TryGetValue(agvAction.ActionType, out var builder))
        {
            return builder.Build(agvAction, context);
        }

        return BuildCustom(agvAction, context);
    }

    /// <summary>
    /// Build custom action defined by manufacturer.
    /// </summary>
    private Models.Orders.OrderAction BuildCustom(AgvAction agvAction, OrderActionContext context)
    {
        var orderAction = new Models.Orders.OrderAction(agvAction.ActionType, agvAction.ActionDescription, agvAction.ResolveBlockingType(context.BlockingType), []);
        if (context.CustomParameters != null)
        {
            orderAction.AddParameters(context.CustomParameters);
        }
        return orderAction;
    }

    /// <summary>
    /// Build the order action directly from the graph definition without using factsheet information.
    /// </summary>
    /// <param name="nodeAction">The node Action.</param>
    /// <returns>The order action.</returns>
    public Models.Orders.OrderAction Build(VehicleTypeNodePropertyAction nodeAction)
    {
        return new Models.Orders.OrderAction(nodeAction.ActionType, null, nodeAction.BlockingType, nodeAction.ActionParameters?.Select(p => new OrderActionParameter() { Key = p.Key, Value=p.Value }).ToList() ?? []);
    }

    /// <summary>
    /// Build the order action directly from the graph definition without using factsheet information.
    /// </summary>
    /// <param name="edgeAction">The edge Action.</param>
    /// <returns>The order action.</returns>
    public Models.Orders.OrderAction Build(VehicleTypeEdgePropertyAction edgeAction)
    {
        return new Models.Orders.OrderAction(edgeAction.ActionType, null, edgeAction.BlockingType, edgeAction.ActionParameters?.Select(p => new OrderActionParameter() { Key = p.Key, Value=p.Value}).ToList() ?? []);
    }
}