using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the errors of the Agv has changed.
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="Errors">All current errors of the Agv</param>
/// <param name="NewErrors">New errors of the Agv</param>
/// <param name="ResolvedErrors">Resolved errors of the Agv</param>
[PublicAPI]
public record AgvErrorChanged(Guid CorrelationId, List<AgvError> Errors, List<AgvError> NewErrors, List<AgvError> ResolvedErrors) : CorrelatedBy<Guid>;