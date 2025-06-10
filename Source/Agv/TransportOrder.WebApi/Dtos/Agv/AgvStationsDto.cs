using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Provides a dto for the agv device-internally reserved stations.
/// </summary>
/// <param name="CorrelationId">The Id of the agv.</param>
/// <param name="Stations">The reserved stations of the agv.</param>
[PublicAPI]
public record AgvStationsDto(Guid CorrelationId, ICollection<string> Stations) : DatedMessage;