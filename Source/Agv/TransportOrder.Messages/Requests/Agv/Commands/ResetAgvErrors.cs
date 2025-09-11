using System.Collections.ObjectModel;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Represents the command sent when the agv errors should be reset.
/// </summary>
/// <param name="CorrelationId">Agv correlation id.</param>
/// <param name="ErrorIds">List of error Ids to reset. If empty, resets all agv errors for the specified agv.</param>
/// <param name="User">The user who reset the errors.</param>
[UsedImplicitly]
public record ResetAgvErrors(Guid CorrelationId, Collection<Guid> ErrorIds, string User) : CorrelatedBy<Guid>;