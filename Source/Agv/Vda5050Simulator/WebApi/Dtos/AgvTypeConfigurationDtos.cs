namespace Gebhardt.Acc.Vda5050Simulator.WebApi.Dtos;

/// <summary>
/// Provides a Dto to change the simulation configuration of an agv type. A property that is omitted keeps its stored value.
/// </summary>
public record UpdateAgvTypeConfigurationDto
{
    public int? NoOfAgvs { get; init; }
    
    public int? StateUpdateIntervalMs { get; init; }
    
    public int? VisualizationUpdateIntervalMs { get; init; }
    
    public double? BatteryDischargeRate { get; init; }
    
    public double? BatteryChargeRate { get; init; }
    
    public int? InitialBatteryCharge { get; init; }
    
    public int? InitialBatteryChargeDeviation { get; init; }

    public double? MaxSpeed { get; init; }
}

/// <summary>
/// Provides a dto to add the simulation configuration of an agv type. A property that is omitted falls back to its default.
/// </summary>
public record AddAgvTypeConfigurationDto : UpdateAgvTypeConfigurationDto
{
    /// <summary>The identifier of the agv type in the form manufacturer.seriesName.</summary>
    public string AgvTypeIdentifier { get; init; } = string.Empty;
}

/// <summary>
/// Provides a dto for the simulation configuration of an agv type.
/// </summary>
public record AgvTypeConfigurationDto
{
    public Guid AgvTypeId { get; init; }

    public string AgvTypeIdentifier { get; init; } = string.Empty;
    
    public string Manufacturer { get; init; } = string.Empty;
    
    public string SeriesName { get; init; } = string.Empty;
    
    public IReadOnlyCollection<string> SerialNumbers { get; init; } = [];
    
    public int NoOfAgvs { get; init; }
    
    public int StateUpdateIntervalMs { get; init; }
    
    public int VisualizationUpdateIntervalMs { get; init; }
    
    public double BatteryDischargeRate { get; init; }
    
    public double BatteryChargeRate { get; init; }
    
    public int InitialBatteryCharge { get; init; }
    
    public int InitialBatteryChargeDeviation { get; init; }

    public double MaxSpeed { get; init; }
}
