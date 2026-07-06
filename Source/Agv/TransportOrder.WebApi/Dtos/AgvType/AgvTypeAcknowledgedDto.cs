using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;

/// <summary>
/// Provides a dto for an agv type acknowledgement.
/// </summary>
/// <param name="Id">The agv type id.</param>
[PublicAPI]
public record AgvTypeAcknowledgedDto(Guid Id): DatedMessage;