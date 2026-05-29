using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;

/// <summary>
/// Represents a command to resend an action to an agv.
/// </summary>
[PublicAPI]
public record ResendDispatchedRouteCommand : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// The route plan to resend.
    /// </summary>
    public RoutePlan Route { get; }

    /// <summary>
    /// Action to be executed.
    /// </summary>
    public AgvAction Action { get; }

    /// <inheritdoc cref="ResendDispatchedRouteCommand" />
    public ResendDispatchedRouteCommand(Guid correlationId, string serialNumber, RoutePlan route, AgvAction action)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Action = action;
        Route = route;
    }
}