using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the creation of an AGV type.
/// </summary>
/// <param name="CorrelationId">The correlation ID of the AGV type.</param>
/// <param name="AgvType">The created AGV type.</param>
public record AgvTypeCreated(Guid CorrelationId, AgvTypeInfo AgvType) : CorrelatedBy<Guid>;