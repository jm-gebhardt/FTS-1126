using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Represents the position status of an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the AGV</param>
/// <param name="SerialNumber">The serial number of the agv.</param>
/// <param name="AgvPosition">AGV position</param>
[UsedImplicitly]
public record AgvPositionStatus(Guid CorrelationId, string SerialNumber, AgvPosition AgvPosition) : IAgv;