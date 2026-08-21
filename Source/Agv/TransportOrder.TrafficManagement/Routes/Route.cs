using Gebhardt.Acc.TransportOrder.TrafficManagement.Providers;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes;

/// <summary>
/// Represents a route for an AGV, consisting of a sequence of RouteSteps.
/// </summary>
internal class Route(IRoutingAgv agv, List<RouteStep> steps, Guid actionCorrelationId)
{
    /// <summary>
    /// Unique identifier for the route.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
    
    /// <summary>
    /// The AGV for which this route is planned.
    /// </summary>
    public IRoutingAgv Agv { get; } = agv;

    /// <summary>
    /// Sequence of RouteSteps that make up this route.
    /// </summary>
    public IReadOnlyCollection<RouteStep> Steps => steps;

    /// <summary>
    /// Calculates the remaining length of the route from the current node to the end of the route.
    /// </summary>
    [UsedImplicitly]
    public double RemainingLength => steps.Where(step => !step.IsCompleted).Sum(step => step.Length);

    /// <summary>
    /// Calculates the remaining driving time for the route from the current node to the end of the route.
    /// </summary>
    public double RemainingDrivingTime => steps.Where(step => !step.IsCompleted).Sum(step => step.DrivingTime);

    /// <summary>
    /// The correlation Id of the action that initiated this route.
    /// </summary>
    public Guid ActionCorrelationId { get; } = actionCorrelationId;

    public override string ToString()
    {
        return $"RouteId: {Id}, AgvId: {Agv.CorrelationId}, ActionCorrelationId: {ActionCorrelationId}, Steps: \n {string.Join("\n", steps.Select(s => s.ToString()))}]";
    }
}