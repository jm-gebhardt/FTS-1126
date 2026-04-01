using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;

/// <summary>
/// Provides a configuration for housekeeping.
/// </summary>
[PublicAPI]
public class HousekeepingConfiguration
{
    /// <summary>
    /// The id of the housekeeping configuration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The value that applies to the <see cref="HousekeepingInterval"/> of an entry becoming outdated.
    /// </summary>
    public int OutdatedValue { get; set; }

    /// <summary>
    /// The interval that applies for outdated entries.
    /// </summary>
    public HousekeepingInterval OutdatedInterval { get; set; }

    private const string ConfigurationSection = "HOUSEKEEPING";
    private const int DefaultOutdatedValue = 14;

    /// <inheritdoc cref="HousekeepingConfiguration"/>
    public HousekeepingConfiguration()
    {
        Id = Guid.NewGuid();
        OutdatedInterval = HousekeepingInterval.Days;
        OutdatedValue = 1;
    }

    /// <inheritdoc cref="HousekeepingConfiguration"/>
    public HousekeepingConfiguration(Guid id, int outdatedValue, HousekeepingInterval outdatedInterval)
    {
        Id = id;
        OutdatedInterval = outdatedInterval;
        OutdatedValue = outdatedValue;
    }

    /// <inheritdoc cref="HousekeepingConfiguration"/>
    public HousekeepingConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationSection);
        Id = Guid.NewGuid();
        OutdatedValue = int.Parse(section.GetValue("OUTDATED_VALUE", DefaultOutdatedValue.ToString()));
        OutdatedInterval = Enum.Parse<HousekeepingInterval>(section.GetValue("OUTDATED_INTERVAL", "Days"));
    }

    /// <summary>
    /// Get the outdated time for housekeeping
    /// </summary>
    /// <returns></returns>
    public DateTime GetOutdatedTime()
    {
        var now = DateTime.UtcNow;
        return OutdatedInterval switch
        {
            HousekeepingInterval.Hours => now.AddHours(-OutdatedValue),
            HousekeepingInterval.Days => now.AddDays(-OutdatedValue),
            HousekeepingInterval.Weeks => now.AddDays(-7 * OutdatedValue),
            HousekeepingInterval.Months => now.AddMonths(-OutdatedValue),
            HousekeepingInterval.Years => now.AddYears(-OutdatedValue),
            _ => now.AddDays(-DefaultOutdatedValue)
        };
    }
}