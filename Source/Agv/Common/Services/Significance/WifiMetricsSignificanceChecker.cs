using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Common.Services.Significance;

/// <summary>
/// Checks if the difference between two <see cref="WifiMetrics"/> instances is significant.
/// </summary>
public class WifiMetricsSignificanceChecker : ReflectionSignificanceChecker<WifiMetrics>
{
    private readonly WifiConfiguration _configuration;

    /// <inheritdoc cref="WifiMetricsSignificanceChecker"/>
    /// <param name="configuration">The configuration for the significance.</param>
    public WifiMetricsSignificanceChecker(WifiConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override Dictionary<string, Func<WifiMetrics, WifiMetrics, bool>> GetSignificantChangeByPropertyName()
    {
        return new Dictionary<string, Func<WifiMetrics, WifiMetrics, bool>>
        {
            { nameof(WifiMetrics.Timestamp), (_, _) => false },
            { nameof(WifiMetrics.BeaconSignalAverage), (obj1, obj2) => Math.Abs(obj1.BeaconSignalAverage.GetValueOrDefault(0) - obj2.BeaconSignalAverage.GetValueOrDefault(0)) >= _configuration.BeaconSignalAverageThreshold },
            { nameof(WifiMetrics.Quality), (obj1, obj2) => Math.Abs(obj1.Quality.GetValueOrDefault(0) - obj2.Quality.GetValueOrDefault(0)) >= _configuration.QualityThreshold },
            { nameof(WifiMetrics.Noise), (obj1, obj2) => Math.Abs(obj1.Noise.GetValueOrDefault(0) - obj2.Noise.GetValueOrDefault(0)) >= _configuration.NoiseThreshold }
        };
    }
}