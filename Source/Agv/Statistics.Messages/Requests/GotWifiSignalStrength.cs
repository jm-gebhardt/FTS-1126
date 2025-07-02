using Gebhardt.Acc.Statistics.Domain.ReadModels;

namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Represents the response to the <see cref="GetWifiSignalStrength"/> request.
/// </summary>
/// <param name="SignalStrength">Heatmap of signal strength data.</param>
public record GotWifiSignalStrength(ScalarHeatmap SignalStrength) : GotHeatmap(Heatmap: SignalStrength);