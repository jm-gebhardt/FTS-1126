using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;

/// <summary>
/// Provides an interface for translating a route step sequence into a wire-level route.
/// </summary>
public interface IRouteStepTranslator
{
    /// <summary>
    /// Translate the given, already-ordered route step executions into a wire-level route.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="steps">The route step executions, in traversal sequence. Must not be empty.</param>
    Task<Models.Orders.Route> Translate(Guid agvCorrelationId, IReadOnlyList<RouteStepExecution> steps);
}
