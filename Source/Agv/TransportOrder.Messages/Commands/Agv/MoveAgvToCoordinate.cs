using Gebhardt.Acc.Common.Messages.ActionGroups;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a request to move an agv to the given coordinates and orientation.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="X">The x coordinate.</param>
/// <param name="Y">The y coordinate.</param>
/// <param name="Theta">The orientation.</param>
/// <param name="Creator">The creator of the action group.</param>
/// <param name="User">The user who moved the agv.</param>
public record MoveAgvToCoordinate(Guid CorrelationId, float X, float Y, float Theta, ActionGroupCreator Creator, string User) : CorrelatedBy<Guid>;