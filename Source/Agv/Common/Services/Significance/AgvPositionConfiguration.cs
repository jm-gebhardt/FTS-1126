using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Filters.Configurations;

namespace Gebhardt.Acc.Common.Services.Significance;

/// <summary>Thresholds for <see cref="AgvPosition"/> comparison.</summary>
public record AgvPositionConfiguration : FilterConfiguration
{
    /// <summary>
    /// The default threshold for position changes.
    /// </summary>
    private const double DefaultPositionThreshold = 0.05;

    /// <summary>
    /// The default threshold for rotation changes. 5 degree in radians.
    /// </summary>
    private const double DefaultPositionRotationThreshold = 0.08726646;

    /// <summary>
    /// The default threshold for position deviation.
    /// </summary>
    private const double DefaultPositionDeviationRangeThreshold = 0.1;

    /// <summary>Thresholds for <see cref="AgvPosition"/> comparison.</summary>
    /// <param name="PositionThreshold">Used for X/Y/Z [m].</param>
    /// <param name="ThetaThreshold">Used Theta [rad].</param>
    /// <param name="DeviationRangeThreshold">Used for DeviationRange property [m].</param>
    public AgvPositionConfiguration(double PositionThreshold = DefaultPositionThreshold, double ThetaThreshold = DefaultPositionRotationThreshold, double DeviationRangeThreshold = DefaultPositionDeviationRangeThreshold)
    {
        this.PositionThreshold = PositionThreshold;
        this.ThetaThreshold = ThetaThreshold;
        this.DeviationRangeThreshold = DeviationRangeThreshold;
    }

    /// <summary>Used for X/Y/Z [m].</summary>
    public double PositionThreshold { get; }

    /// <summary>Used Theta [rad].</summary>
    public double ThetaThreshold { get; }

    /// <summary>Used for DeviationRange property [m].</summary>
    public double DeviationRangeThreshold { get; }
}