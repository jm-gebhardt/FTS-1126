using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;

/// <summary>
/// Provides the boundaries a simulation configuration must stay within.
/// </summary>
[PublicAPI]
public static class SimulationLimits
{
    /// <summary>
    /// The highest number of AGVs that can be simulated for a single AGV type.
    /// </summary>
    public const int MaxAgvsPerType = 100;

    /// <summary>
    /// The shortest interval in milliseconds allowed between two publications.
    /// </summary>
    public const int MinUpdateIntervalMs = 10;

    /// <summary>
    /// The lowest battery charge in percent.
    /// </summary>
    public const int MinBatteryChargePercent = 0;

    /// <summary>
    /// The highest battery charge in percent.
    /// </summary>
    public const int MaxBatteryChargePercent = 100;
}