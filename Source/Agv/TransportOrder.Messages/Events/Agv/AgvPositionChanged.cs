using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the current position of an agv has changed.
/// </summary>
/// <param name="CorrelationId">The correlationId of the new agv position.</param>
/// <param name="NewAgvPosition">The new agv position.</param>
/// <param name="PreviousAgvPosition">The previous agv position.</param>
/// <param name="SerialNumber">The serial number of the agv.</param>
[PublicAPI]
public record AgvPositionChanged(Guid CorrelationId, AgvPosition NewAgvPosition, AgvPosition? PreviousAgvPosition, string SerialNumber) : CorrelatedBy<Guid>, IAgv;