using System.Collections.ObjectModel;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Represents the command sent when the agv errors should be reset.
/// </summary>
/// <param name="CorrelationId">Agv correlation id.</param>
/// <param name="ErrorIds">List of error Ids to reset. If empty, resets all agv errors for the specified agv.</param>
[UsedImplicitly]
public record ResetAgvErrors(Guid CorrelationId, Collection<Guid> ErrorIds) : CorrelatedBy<Guid>;