using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.WCSCommunication.Messages.Commands;

/// <summary>
///Provides a command to handle the failure of the host order cancellation.
/// </summary>
[PublicAPI]
public record HandleCancellationFailure(Guid CorrelationId, HostOrderCancelFailedErrorCode Error) : CorrelatedBy<Guid>;