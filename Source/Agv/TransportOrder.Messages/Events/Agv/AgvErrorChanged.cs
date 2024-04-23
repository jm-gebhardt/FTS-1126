using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the errors of the Agv has changed
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="Errors"></param>
[PublicAPI]
public record AgvErrorChanged(Guid CorrelationId, List<AgvError> Errors) : CorrelatedBy<Guid>;