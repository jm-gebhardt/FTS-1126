using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;

/// <summary>
/// Provides a dto for agv types.
/// </summary>
[PublicAPI]
public record AgvTypeDto
{
    /// <summary>
    /// The unique identifier of the AGV type, derived from its identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the AGV type, consisting of manufacturer and series name.
    /// </summary>
    public string AgvTypeIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The manufacturer of the AGV type.
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// The series name of the AGV type.
    /// </summary>
    public string SeriesName { get; set; } = string.Empty;

    /// <summary>
    /// The length of the AGV type in [m].
    /// </summary>
    public double Length { get; set; }

    /// <summary>
    /// The width of the AGV type in [m].
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Whether the AGV type has been acknowledged (confirmed) by an operator.
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// Whether the AGV type was reported by an AGV itself.
    /// </summary>
    public bool ReportedByAgv { get; init; }

    /// <summary>
    /// Whether at least one AGV of this type has a graph.
    /// </summary>
    public bool HasGraph { get; set; }

    /// <summary>
    /// Whether this agv type has a factsheet.
    /// </summary>
    public bool HasFactsheet => Factsheet != null;

    /// <summary>
    /// The factsheet of this agv type.
    /// </summary>
    public string? Factsheet { get; set; }
}