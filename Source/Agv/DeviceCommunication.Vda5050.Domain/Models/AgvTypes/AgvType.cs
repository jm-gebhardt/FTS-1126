using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;

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

    /// <summary>
    /// The entire factsheet JSON the AGV type was created from.
    /// </summary>
    public string RawData { get; init; }

    /// <inheritdoc/>
    public bool Acknowledged { get; init; }

    /// <inheritdoc cref="AgvType"/>
    public AgvType(string agvTypeIdentifier, string manufacturer, string seriesName, double length, double width, string rawData, bool acknowledged)
    {
        Id = agvTypeIdentifier.ToGuid();
        AgvTypeIdentifier = agvTypeIdentifier;
        Manufacturer = manufacturer;
        SeriesName = seriesName;
        Length = length;
        Width = width;
        RawData = rawData;
        Acknowledged = acknowledged;
    }

    /// <summary>For AutoMapper.</summary>
    public AgvType()
    {
        AgvTypeIdentifier = null!;
        Manufacturer = null!;
        SeriesName = null!;
        RawData = null!;
    }
}