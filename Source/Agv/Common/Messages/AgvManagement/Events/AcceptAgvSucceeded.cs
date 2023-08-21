using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// AcceptAgv was successful
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record AcceptAgvSucceeded(Guid CorrelationId) : CorrelatedBy<Guid>;