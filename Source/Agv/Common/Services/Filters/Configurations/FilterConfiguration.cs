namespace Gebhardt.Acc.Common.Services.Filters.Configurations;

/// <summary>
/// Provides Min/MaxInterval configuration for filters.
/// </summary>
public record FilterConfiguration
{
    /// <summary>
    /// Specifies the minimum elapsed time that should be considered before a new object is eligible for processing, regardless of whether it meets the usual criteria for significance or difference.
    /// </summary>
    public TimeSpan MinInterval { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Defines the maximum time that can elapse before a new object must be processed, regardless of whether it meets the usual criteria for significance or difference.
    /// </summary>
    public TimeSpan MaxInterval { get; init; } = TimeSpan.FromSeconds(10);
}