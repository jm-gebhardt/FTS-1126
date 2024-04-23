using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv online status
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="IsNowOnline"></param>
public record AgvOnlineStatusDto([PublicAPI] Guid CorrelationId, bool IsNowOnline) : DatedMessage;