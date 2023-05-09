using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;

/// <summary>
/// Confirms the successful queuing of the order
/// </summary>
[UsedImplicitly]
public record OrderQueuedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;