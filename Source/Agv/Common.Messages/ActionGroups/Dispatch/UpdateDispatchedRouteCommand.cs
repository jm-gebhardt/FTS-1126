using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;

/// <summary>
/// Represents a command to update the action on the agv side.
/// </summary>
[PublicAPI]
public record UpdateDispatchedRouteCommand : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// The updated route plan for this action.
    /// </summary>
    public RoutePlan Route { get; }

    /// <summary>
    /// Action to be executed.
    /// </summary>
    public AgvAction Action { get; }

    /// <inheritdoc cref="UpdateDispatchedRouteCommand" />
    public UpdateDispatchedRouteCommand(Guid correlationId, string serialNumber, RoutePlan route, AgvAction action)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Action = action;
        Route = route;
    }
}