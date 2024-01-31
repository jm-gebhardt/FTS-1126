using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the current position of an agv has changed.
/// </summary>
/// <param name="CorrelationId">The correlationId of the new agv position.</param>
/// <param name="NewAgvPosition">The new agv position.</param>
/// <param name="PreviousAgvPosition">The previous agv position.</param>
/// <param name="SerialNumber">The serial number of the agv.</param>
[PublicAPI]
public record AgvPositionChanged(Guid CorrelationId, AgvPosition NewAgvPosition, AgvPosition? PreviousAgvPosition, string SerialNumber) : CorrelatedBy<Guid>, IAgv;