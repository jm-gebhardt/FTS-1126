using CommonAgvPosition = Gebhardt.Acc.Common.Domain.Models.AgvPosition;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

/// <summary>
/// Agv position.
/// </summary>
public record AgvPosition : CommonAgvPosition
{
    /// <summary>
    /// The quality of the localization and can therefore be used, e.g., by SLAM mobile robots to describe how accurate the current position information is [0..1];
    /// </summary>
    public double? LocalizationScore { get; init; }

    /// <summary>
    /// The id of the map (vehicle map.)
    /// </summary>
    public string MapId { get; init; } = string.Empty;
}