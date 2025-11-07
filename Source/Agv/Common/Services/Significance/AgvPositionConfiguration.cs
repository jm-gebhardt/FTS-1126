using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Filters.Configurations;

namespace Gebhardt.Acc.Common.Services.Significance;

/// <summary>Thresholds for <see cref="AgvPosition"/> comparison.</summary>
/// <param name="PositionThreshold">Used for X/Y/Z [m].</param>
/// <param name="ThetaThreshold">Used Theta [rad].</param>
/// <param name="DeviationRangeThreshold">Used for DeviationRange property [m].</param>
public record AgvPositionConfiguration(double PositionThreshold, double ThetaThreshold, double DeviationRangeThreshold) : FilterConfiguration;