using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv locked status
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="IsNowLocked"></param>
public record AgvLockedStatusDto([PublicAPI] Guid CorrelationId, bool IsNowLocked) : DatedMessage;