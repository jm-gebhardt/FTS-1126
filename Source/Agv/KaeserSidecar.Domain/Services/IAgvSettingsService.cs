namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <summary>
/// Provides a service for agv settings.
/// </summary>
public interface IAgvSettingsService
{
    /// <summary>
    /// True, if the agvs battery level is low or less.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv</param>
    Task<bool> CheckAgvHasLowBatteryLevel(Guid agvCorrelationId);
}