using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Statistics.Messages.Commands;

/// <summary>
/// Provides a command to add Wi-Fi metrics.
/// </summary>
/// <param name="Metrics">The Wi-Fi metrics to be added.</param>
/// <param name="GridPositionIds">The id of the agv grid positions.</param>
public record AddWifiMetrics(WifiMetrics Metrics, List<Guid> GridPositionIds);