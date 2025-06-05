using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Provides a dto for the agv zones.
/// </summary>
/// <param name="CorrelationId">The Id of the agv.</param>
/// <param name="Zones">The zones of the agv.</param>
[PublicAPI]
public record AgvZonesDto(Guid CorrelationId, ICollection<string> Zones) : DatedMessage;