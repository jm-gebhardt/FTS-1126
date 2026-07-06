using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

/// <summary>
/// An AGV type describing a group(specific type) of AGVs.
/// </summary>
[PublicAPI]
public record AgvType : IAgvType
{
    /// <summary>
    /// The unique identifier of the AGV type, derived from its identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <inheritdoc/>
    public string AgvTypeIdentifier { get; init; }

    /// <inheritdoc/>
    public string Manufacturer { get; init; }

    /// <inheritdoc/>
    public string SeriesName { get; init; }

    /// <inheritdoc/>
    public double Length { get; init; }

    /// <inheritdoc/>
    public double Width { get; init; }

    /// <inheritdoc/>
    public bool Acknowledged { get; private set; }

    /// <inheritdoc cref="AgvType"/>
    public AgvType(string agvTypeIdentifier, string manufacturer, string seriesName, double length, double width, bool acknowledged)
    {
        Id = agvTypeIdentifier.ToGuid();
        AgvTypeIdentifier = agvTypeIdentifier;
        Manufacturer = manufacturer;
        SeriesName = seriesName;
        Length = length;
        Width = width;
        Acknowledged = acknowledged;
    }

    /// <summary>For serialization.</summary>
    public AgvType()
    {
        AgvTypeIdentifier = null!;
        Manufacturer = null!;
        SeriesName = null!;
    }

    /// <summary>
    /// Acknowledge the agv type.
    /// </summary>
    public void Acknowledge()
    {
        Acknowledged = true;
    }
}