using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.WCSCommunication.Messages.Commands;

/// <summary>
/// Provides a command to handle the failure of a transport order.
/// </summary>
[PublicAPI]
public record HandleHostOrderFailure(Guid CorrelationId, HostOrderHandlingFailedErrorCode Error, DateTime Timestamp) : CorrelatedBy<Guid>;