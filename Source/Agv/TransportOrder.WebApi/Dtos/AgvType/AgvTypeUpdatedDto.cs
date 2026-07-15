using Gebhardt.Acc.Common.Messages;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;

/// <summary>
/// Represents the message sent to clients when an agv type has been updated. 
/// </summary>
/// <param name="AgvType">The new agv type.</param>
public record AgvTypeUpdatedDto(AgvTypeDto AgvType): DatedMessage;