using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Vda5050Simulator.Domain;

/// <summary>
/// Describes how many AGVs of one AGV type are simulated and how they behave.
/// </summary>
public record AgvTypeConfiguration
{
    private const char IdentifierSeparator = '.';

    public const int DefaultStateUpdateIntervalMs = 5000;
    public const int DefaultVisualizationUpdateIntervalMs = 800;
    public const double DefaultBatteryDischargeRate = 0.1;
    public const double DefaultBatteryChargeRate = 1.0;
    public const int DefaultInitialBatteryCharge = 75;
    public const int DefaultInitialBatteryChargeDeviation = 5;
    public const double DefaultMaxSpeed = 1.0;
    public const int MaxAgvsPerType = 100;
    public const int MinUpdateIntervalMs = 10;
    public const int MaxIdentifierLength = 400;

    /// <summary>The unique identifier of the configuration, derived from the AGV type identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>The identifier of the simulated AGV type in the form manufacturer.seriesName.</summary>
    public string AgvTypeIdentifier { get; init; }

    public int NoOfAgvs { get; init; }

    public int StateUpdateIntervalMs { get; init; }

    public int VisualizationUpdateIntervalMs { get; init; }

    public double BatteryDischargeRate { get; init; }

    public double BatteryChargeRate { get; init; }

    public int InitialBatteryCharge { get; init; }

    public int InitialBatteryChargeDeviation { get; init; }

    public double MaxSpeed { get; init; }

    public string Manufacturer => IdentifierPart(AgvTypeIdentifier, 0);

    public string SeriesName => IdentifierPart(AgvTypeIdentifier, 1);

    public AgvTypeConfiguration()
    {
        AgvTypeIdentifier = null!;
    }

    public AgvTypeConfiguration(string agvTypeIdentifier,
        int noOfAgvs = 0,
        int stateUpdateIntervalMs = DefaultStateUpdateIntervalMs,
        int visualizationUpdateIntervalMs = DefaultVisualizationUpdateIntervalMs,
        double batteryDischargeRate = DefaultBatteryDischargeRate,
        double batteryChargeRate = DefaultBatteryChargeRate,
        int initialBatteryCharge = DefaultInitialBatteryCharge,
        int initialBatteryChargeDeviation = DefaultInitialBatteryChargeDeviation,
        double maxSpeed = DefaultMaxSpeed)
    {
        Id = agvTypeIdentifier.ToGuid();
        AgvTypeIdentifier = agvTypeIdentifier;
        NoOfAgvs = noOfAgvs;
        StateUpdateIntervalMs = stateUpdateIntervalMs;
        VisualizationUpdateIntervalMs = visualizationUpdateIntervalMs;
        BatteryDischargeRate = batteryDischargeRate;
        BatteryChargeRate = batteryChargeRate;
        InitialBatteryCharge = initialBatteryCharge;
        InitialBatteryChargeDeviation = initialBatteryChargeDeviation;
        MaxSpeed = maxSpeed;
    }

    private static string IdentifierPart(string? agvTypeIdentifier, int index)
    {
        string[] parts = agvTypeIdentifier?.Split(IdentifierSeparator, 2) ?? [];
        return parts.Length > index ? parts[index] : string.Empty;
    }

    /// <summary>The serial numbers of the AGVs simulated for this AGV type.</summary>
    public IEnumerable<string> SerialNumbers()
    {
        return Enumerable.Range(0, NoOfAgvs).Select(index => $"{SeriesName}{index}");
    }

    /// <summary>Returns the validation errors of this configuration.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Manufacturer) || string.IsNullOrWhiteSpace(SeriesName))
        {
            errors.Add($"The AGV type identifier '{AgvTypeIdentifier}' is not in the form manufacturer.seriesName.");
        }
        if (AgvTypeIdentifier.Length > MaxIdentifierLength)
        {
            errors.Add($"The AGV type identifier must not be longer than {MaxIdentifierLength} characters but was {AgvTypeIdentifier.Length}.");
        }
        if (NoOfAgvs is < 0 or > MaxAgvsPerType)
        {
            errors.Add($"The number of simulated AGVs must be between 0 and {MaxAgvsPerType} but was {NoOfAgvs}.");
        }
        if (StateUpdateIntervalMs < MinUpdateIntervalMs)
        {
            errors.Add($"The state update interval must be at least {MinUpdateIntervalMs} milliseconds but was {StateUpdateIntervalMs}.");
        }
        if (VisualizationUpdateIntervalMs < MinUpdateIntervalMs)
        {
            errors.Add($"The visualization update interval must be at least {MinUpdateIntervalMs} milliseconds but was {VisualizationUpdateIntervalMs}.");
        }
        if (InitialBatteryCharge is < 0 or > 100)
        {
            errors.Add($"The initial battery charge must be between 0 and 100 percent but was {InitialBatteryCharge}.");
        }
        if (InitialBatteryChargeDeviation is < 0 or > 100)
        {
            errors.Add($"The initial battery charge deviation must be between 0 and 100 percent but was {InitialBatteryChargeDeviation}.");
        }
        if (BatteryDischargeRate <= 0)
        {
            errors.Add($"The battery discharge rate must be greater than 0 percent per second but was {BatteryDischargeRate}.");
        }
        if (BatteryChargeRate <= 0)
        {
            errors.Add($"The battery charge rate must be greater than 0 percent per second but was {BatteryChargeRate}.");
        }
        if (MaxSpeed <= 0)
        {
            errors.Add($"The maximum speed must be greater than 0 meters per second but was {MaxSpeed}.");
        }
        return errors;
    }
}

public static class AgvTypeConfigurationEx
{
    public static AgvConfiguration ToAgvConfiguration(this AgvTypeConfiguration typeConfiguration, string serialNumber)
    {
        return new AgvConfiguration(typeConfiguration.Manufacturer, typeConfiguration.SeriesName, serialNumber)
        {
            BatteryChargeRate = typeConfiguration.BatteryChargeRate,
            BatteryDischargeRate = typeConfiguration.BatteryDischargeRate,
            InitialBatteryCharge = typeConfiguration.InitialBatteryCharge,
            InitialBatteryChargeDeviation = typeConfiguration.InitialBatteryChargeDeviation,
            StateUpdateIntervalMs = typeConfiguration.StateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = typeConfiguration.VisualizationUpdateIntervalMs,
            MaxSpeed = typeConfiguration.MaxSpeed
        };
    }
}