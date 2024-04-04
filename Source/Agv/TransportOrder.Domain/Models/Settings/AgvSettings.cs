using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

// Disabled because properties are reset by reflection (not visible to resharper)
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;

/// <summary>
/// Agv (group) specific settings.
/// </summary>
public class AgvSettings
{
    /// <inheritdoc cref="AgvSettings" />
    public AgvSettings(Guid groupId)
    {
        Id = Guid.NewGuid();
        AgvGroupId = groupId;
    }

    /// <summary>
    /// Ctor for EfCore.
    /// </summary>
    [UsedImplicitly]
    private AgvSettings() { }

    /// <summary>
    /// The uid of the agv settings
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Agv group the setting applies to
    /// </summary>
    public Guid AgvGroupId { get; init; }

    // Foreign keys to default settings
    /// <summary>Timeout value [ms] to set AGV offline while it is in busy mode</summary>
    public Guid AgvOfflineTimeoutMsWhileIdleId { get; set; }

    /// <summary>Below this threshold only charge actions will be started.</summary>
    public Guid AgvOfflineTimeoutMsWhileBusyId { get; set; }

    /// <summary>
    /// Reference to <see cref="AgvOfflineTimeoutMsWhileBusy"/>.
    /// </summary>
    public Guid BatteryLevelCriticalId { get; set; }

    /// <summary>
    /// Reference to <see cref="AgvOfflineTimeoutMsWhileBusy"/>.
    /// </summary>
    public Guid BatteryLevelLowId { get; set; }

    /// <summary>
    /// Reference to <see cref="AgvOfflineTimeoutMsWhileBusy"/>.
    /// </summary>
    public Guid MinChargeUpId { get; set; }

    /// <summary>
    /// Reference to <see cref="AgvOfflineTimeoutMsWhileBusy"/>.
    /// </summary>
    public Guid MaxChargeUpId { get; set; }

    /// Group specific settings
    /// <summary>Timeout value [ms] to set AGV offline while it is in busy mode</summary>
    public Setting AgvOfflineTimeoutMsWhileIdle { get; set; } = null!;

    /// <summary>Below this threshold only charge actions will be started.</summary>
    public Setting AgvOfflineTimeoutMsWhileBusy { get; set; } = null!;

    /// <summary>Below this threshold only charge actions will be started.</summary>
    public Setting BatteryLevelCritical { get; set; } = null!;

    /// <summary>Below this threshold only ActionGroups are started when the system is under high load.</summary>
    public Setting BatteryLevelLow { get; set; } = null!;

    /// <summary>Once charging is started it will at least charge up to this level.</summary>
    public Setting MinChargeUp { get; set; } = null!;

    /// <summary>We stop charging at this level actively.</summary>
    public Setting MaxChargeUp { get; set; } = null!;

    /// <summary>
    /// Update all valid settings.
    /// </summary>
    /// <param name="settings">Updated settings.</param>
    /// <param name="isDefault">True if new settings are for a default group.</param>
    public void Update(AgvSettingsReadModel settings, bool isDefault)
    {
        foreach ((string name, object? value) in settings.GetValidPropertiesInformation())
        {
            var settingsProperty = GetType()
                .GetProperties()
                .SingleOrDefault(p => p.PropertyType == typeof(Setting) && p.Name.ToUpper() == name.ToUpper());

            var setting = (Setting?)settingsProperty!.GetValue(this);
            bool isNewAgvSpecificValue = setting == null || (!isDefault && setting.IsDefault && setting.Value != value!.ToString())!;
            if (isNewAgvSpecificValue)
            {
                setting = new Setting(Guid.NewGuid(), name, value!.ToString()!, value.GetType().FullName!, isDefault);
                UpdatePropertyId(name, setting.Id);
            }
            setting!.Value = value!.ToString()!;
            settingsProperty.SetValue(this, setting);
        }
    }

    /// <summary>
    /// Get the setting for the given name.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <returns>The setting.</returns>
    /// <exception cref="InvalidAgvSettingsPropertyException">Thrown, if the setting was not found. </exception>
    public Setting GetSettingByName(string name)
    {
        var setting = GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(Setting))
            .Select(p => (Setting?)p.GetValue(this))
            .SingleOrDefault(s => s?.Name.ToUpper() == name.ToUpper());
        return setting ?? throw new InvalidAgvSettingsPropertyException(name);
    }

    /// <summary>
    /// Update a property of type <see cref="Guid"/> with a new value.
    /// </summary>
    /// <param name="name">The name of the referenced property.</param>
    /// <param name="newValue">The new value.</param>
    /// <exception cref="InvalidAgvSettingsPropertyException"></exception>
    public void UpdatePropertyId(string name, Guid newValue)
    {
        var id = GetType()
            .GetProperties()
            .SingleOrDefault(p => p.PropertyType ==  typeof(Guid)  && p.Name.ToUpper() == $"{name}Id".ToUpper());

        if (id == null)
        {
            throw new InvalidAgvSettingsPropertyException(name);
        }

        id.SetValue(this, newValue);
    }

    /// <summary>
    /// Convert to <see cref="AgvSettingsReadModel"/>
    /// </summary>
    /// <returns></returns>
    public AgvSettingsReadModel ToReadModel()
    {
        var readModel = new AgvSettingsReadModel();

        readModel.BatteryLevelLow = ConvertToType(BatteryLevelLow.ValueType, BatteryLevelLow.Value, readModel.BatteryLevelLow);
        readModel.BatteryLevelCritical = ConvertToType(BatteryLevelCritical.ValueType, BatteryLevelCritical.Value, readModel.BatteryLevelCritical);
        readModel.MinChargeUp = ConvertToType(MinChargeUp.ValueType, MinChargeUp.Value, readModel.MinChargeUp);
        readModel.MaxChargeUp = ConvertToType(MaxChargeUp.ValueType, MaxChargeUp.Value, readModel.MaxChargeUp);
        readModel.AgvOfflineTimeoutMsWhileBusy = ConvertToType(AgvOfflineTimeoutMsWhileBusy.ValueType, AgvOfflineTimeoutMsWhileBusy.Value, readModel.AgvOfflineTimeoutMsWhileBusy);
        readModel.AgvOfflineTimeoutMsWhileIdle = ConvertToType(AgvOfflineTimeoutMsWhileIdle.ValueType, AgvOfflineTimeoutMsWhileIdle.Value, readModel.AgvOfflineTimeoutMsWhileIdle);
        return readModel;
    }

    private T ConvertToType<T>(string valueType, string value, T defaultValue)
    {
        var type = Type.GetType(valueType);

        if (valueType.IsNullOrEmpty() || value.IsNullOrEmpty() || type == null)
        {
            return defaultValue;
        }
        return (T)Convert.ChangeType(value, type);
    }
}