using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>Represents a command to add or update an AGV type.</summary>
/// <param name="CorrelationId">The correlation ID of the AGV type.</param>
/// <param name="AgvType">The AGV type to add or update.</param>
[PublicAPI]
public record AddOrUpdateAgvType(Guid CorrelationId, AgvTypeInfo AgvType) : CorrelatedBy<Guid>;