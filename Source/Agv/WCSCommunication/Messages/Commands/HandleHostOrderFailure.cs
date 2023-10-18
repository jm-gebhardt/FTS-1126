using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Commands;

/// <summary>
/// Provides a command to handle the failure of a transport order.
/// </summary>
[PublicAPI]
public record HandleHostOrderFailure(Guid CorrelationId, HostOrderHandlingFailedErrorCode Error, DateTime Timestamp) : CorrelatedBy<Guid>;
