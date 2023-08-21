using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// AcceptAgv was rejected
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record AcceptAgvRejected(Guid CorrelationId) : CorrelatedBy<Guid>;