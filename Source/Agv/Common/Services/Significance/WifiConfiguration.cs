using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Filters.Configurations;

namespace Gebhardt.Acc.Common.Services.Significance;

/// <summary>Thresholds for <see cref="WifiMetrics"/> comparison.</summary>
/// <param name="BeaconSignalAverageThreshold">Threshold for beacon signal average changes to be considered significant [dBm].</param>
/// <param name="QualityThreshold">Threshold for quality changes to be considered significant.</param>
/// <param name="NoiseThreshold">Threshold for noise level changes to be considered significant [dBm].</param>
public record WifiConfiguration(double BeaconSignalAverageThreshold, double QualityThreshold, double NoiseThreshold) : FilterConfiguration;