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
    public bool Acknowledged { get; set; }

    /// <inheritdoc/>
    public bool ReportedByAgv { get; init; }

    /// <inheritdoc/>
    public string RawData { get; init; }

    /// <summary>
    /// Collection of load states supported by this AGV type. Hardcoded to two states: loaded and unloaded. This is a temporary solution until the load states are derived from factsheet/loadSpecification.
    /// </summary>
    public IReadOnlyCollection<LoadState> LoadStates { get; init; } 

    /// <inheritdoc cref="AgvType"/>
    public AgvType(string agvTypeIdentifier,
        string manufacturer,
        string seriesName,
        double length,
        double width,
        bool acknowledged,
        bool reportedByAgv,
        string rawData)
    {
        Id = IdFromIdentifier(agvTypeIdentifier);
        AgvTypeIdentifier = agvTypeIdentifier;
        Manufacturer = manufacturer;
        SeriesName = seriesName;
        Length = length;
        Width = width;
        Acknowledged = acknowledged;
        ReportedByAgv = reportedByAgv;
        RawData = rawData;
        LoadStates = new List<LoadState>()
        {
            new($"{agvTypeIdentifier}_unloaded".ToGuid(), false),
            new($"{agvTypeIdentifier}_loaded".ToGuid(), true)
        };
    }

    /// <summary>For serialization.</summary>
    public AgvType()
    {
        AgvTypeIdentifier = null!;
        Manufacturer = null!;
        SeriesName = null!;
        LoadStates = null!;
        RawData = "";
    }

    /// <summary>
    /// Derives the AGV type id from its identifier.
    /// </summary>
    /// <param name="agvTypeIdentifier">The AGV type identifier.</param>
    /// <returns>The id derived from the identifier.</returns>
    public static Guid IdFromIdentifier(string agvTypeIdentifier) => agvTypeIdentifier.ToGuid();

    /// <summary>
    /// Acknowledge the agv type.
    /// </summary>
    public void Acknowledge()
    {
        Acknowledged = true;
    }
}