using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv removed
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
public record AgvRemovedDto([PublicAPI] Guid CorrelationId) : DatedMessage;