using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order;

/// <summary>
/// Provides an interface for order building services.
/// </summary>
public interface IOrderBuilder
{
    /// <summary>
    /// Build an order based on a fleet manager agv action and agv graph and fact sheet information.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="routePlan">The route plan.</param>
    /// <param name="action">The fleet manager agv action.</param>
    /// <returns>An order.</returns>
    Task<Models.Orders.Order> Build(Guid agvCorrelationId, RoutePlan routePlan, AgvAction action);
}