using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

/// <summary>
/// Provides a dto to add the simulation configuration of an agv type. A property that is omitted falls back to its default.
/// </summary>
[PublicAPI]
public record AddAgvTypeConfigurationDto : UpdateAgvTypeConfigurationDto
{
    /// <summary>
    /// The identifier of the agv type in the form manufacturer.seriesName.
    /// </summary>
    public string AgvTypeIdentifier { get; init; } = string.Empty;
}