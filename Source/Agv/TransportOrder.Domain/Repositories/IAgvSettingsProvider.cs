using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides agv (group) specific settings.
/// </summary>
public interface IAgvSettingsProvider
{
    /// <summary>
    /// Get settings for an agv settings group.
    /// </summary>
    /// <param name="agvSettingsGroup">The agv settings group id.</param>
    AgvSettingsReadModel GetSettingsForAgvGroup(Guid agvSettingsGroup);

    /// <summary>
    /// Get the most specific settings group of an agv.
    /// Group type hierarchy: All -> SerialNumber
    /// If an agv is provided and a group for its serial number exists, that group is returned.
    /// is returned.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <returns>The most specific agv group.</returns>
    Task<AgvSettingsGroup> GetSettingsGroupForAgv(Models.Agv.Agv? agv=null);

    /// <summary>
    /// Initialize the agv settings group default.
    /// </summary>
    Task InitializeSettingsGroups();

    /// <summary>
    /// Adds or updates values for an agv group given by identifier and type.
    /// </summary>
    /// <param name="newSettings">The updated settings.</param>
    /// <param name="type">The group type.</param>
    /// <param name="identifier">The group identifier.</param>
    Task AddOrUpdateSettingsForAgvGroup(AgvSettingsReadModel newSettings, GroupType type, string identifier);
}