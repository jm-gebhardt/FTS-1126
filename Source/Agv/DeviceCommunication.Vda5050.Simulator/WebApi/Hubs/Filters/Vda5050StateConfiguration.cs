using Gebhardt.Acc.Common.Services.Filters.Configurations;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs.Filters;

/// <summary>
/// Thresholds and intervals for filtering VDA5050 state broadcasts.
/// </summary>
[PublicAPI]
public record Vda5050StateConfiguration : FilterConfiguration
{
    /// <summary>Minimum battery charge change [%] to be significant.</summary>
    public double BatteryChargeThreshold { get; init; } = 1.0;

    /// <summary>Minimum X/Y change [m] to be significant.</summary>
    public double PositionThreshold { get; init; } = 0.05;

    /// <summary>Minimum Theta change [rad] to be significant.</summary>
    public double ThetaThreshold { get; init; } = 5 * Math.PI / 180;
}