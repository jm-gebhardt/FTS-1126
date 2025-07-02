using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the Wi-Fi metrics of an AGV.
/// </summary>
/// <param name="CorrelationId">The correlation ID of the AGV.</param>
/// <param name="SerialNumber">The serial number of the AGV.</param>
/// <param name="AgvPosition">The current position of the AGV.</param>
/// <param name="Metrics">Metrics related to the AGV's Wi-Fi connection</param>
[UsedImplicitly]
public record AgvWifiMetricsReceived(Guid CorrelationId, string SerialNumber, AgvPosition AgvPosition, WifiMetrics Metrics) : DatedMessage, IAgvSerialNumber, IWithAgvPosition;