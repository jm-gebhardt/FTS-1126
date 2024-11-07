using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Provides a dto for an agv mode.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="AgvMode">The agv mode.</param>
[PublicAPI]
public record AgvModeChangedDto(Guid CorrelationId, AgvModeDto AgvMode): DatedMessage;