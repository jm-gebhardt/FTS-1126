using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;

/// <summary>
/// Represents a command to trigger abortion of an action on the agv side and start of a new action.
/// </summary>
[PublicAPI]
public record AbortAndDispatchRouteCommand : CorrelatedBy<Guid>, IAgv
{
    /// <summary>
    /// Action to be executed.
    /// </summary>
    public AgvAction Action { get; }

    /// <summary>
    /// The route plan for the new action.
    /// </summary>
    public RoutePlan Route { get; }

    /// <summary>
    /// The correlation id of the aborted action.
    /// </summary>
    public Guid? AbortedActionCorrelationId { get; }

    /// <inheritdoc cref="IAgv" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <inheritdoc cref="AbortAndDispatchRouteCommand" />
    public AbortAndDispatchRouteCommand(Guid correlationId, string serialNumber, AgvAction action, RoutePlan route, Guid? abortedActionCorrelationId)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Action = action;
        Route = route;
        AbortedActionCorrelationId = abortedActionCorrelationId;
    }
}