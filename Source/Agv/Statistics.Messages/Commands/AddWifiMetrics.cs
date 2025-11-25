using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Statistics.Messages.Commands;

/// <summary>
/// Provides a command to add Wi-Fi metrics.
/// </summary>
/// <param name="AgvCorrelationId">The Agv CorrelationId.</param>
/// <param name="Metrics">The Wi-Fi metrics to be added.</param>
/// <param name="GridCellIds">The id of the grid cells.</param>
public record AddWifiMetrics(Guid AgvCorrelationId, WifiMetrics Metrics, List<Guid> GridCellIds);