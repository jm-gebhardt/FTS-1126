using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Settings;

/// <summary>
/// Represents an agv group that shares a setting.
/// </summary>
public class AgvSettingsGroup
{
    /// <summary>
    /// The group id.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The group type.
    /// </summary>
    public GroupType GroupType { get; init; }

    /// <summary>
    /// The identifier of the group (empty for type <see cref="GroupType.All"/>)
    /// </summary>
    public string Identifier { get; init; } = "";

    /// <summary>
    /// The <see cref="AgvSetting"/> of the group.
    /// </summary>
    public AgvSettings AgvSetting { get; init; } = null!;

    /// <inheritdoc cref="AgvSettingsGroup"/>>
    public AgvSettingsGroup(GroupType type, string identifier="")
    {
        Id = Guid.NewGuid();
        GroupType = type;
        Identifier = identifier;
        AgvSetting = new AgvSettings(Id);
    }

    /// <summary>
    /// Ctor for EfCore.
    /// </summary>
    [UsedImplicitly]
    private AgvSettingsGroup(){}

    /// <summary>
    /// Update the agv settings for the group.
    /// </summary>
    /// <param name="settings">New settings.</param>
    public void UpdateSettings(AgvSettingsReadModel settings)
    {
        bool isDefault = GroupType == GroupType.All; 
        AgvSetting.Update(settings, isDefault);
    }
}

/// <summary>
/// Hierarchical group type.
/// All -> SerialNumber
/// </summary>
public enum GroupType
{
    /// <summary>
    /// All agvs (default).
    /// </summary>
    All,

    /// <summary>
    /// Agvs with the same <see cref="Agv.Agv.SerialNumber"/>.
    /// </summary>
    SerialNumber
}