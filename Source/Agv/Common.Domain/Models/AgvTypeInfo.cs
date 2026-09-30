using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Carries the descriptor of an AGV type between services.
/// </summary>
[PublicAPI]
public record AgvTypeInfo : IAgvType
{
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
    public bool Acknowledged { get; init; }

    /// <inheritdoc/>
    public bool ReportedByAgv { get; init; }

    /// <inheritdoc/>
    public string RawData { get; init; }

    /// <inheritdoc cref="AgvTypeInfo"/>
    public AgvTypeInfo(string agvTypeIdentifier,
        string manufacturer,
        string seriesName,
        double length,
        double width,
        bool acknowledged,
        string rawData,
        bool reportedByAgv)
    {
        AgvTypeIdentifier = agvTypeIdentifier;
        Manufacturer = manufacturer;
        SeriesName = seriesName;
        Length = length;
        Width = width;
        Acknowledged = acknowledged;
        ReportedByAgv = reportedByAgv;
        RawData = rawData;
    }
}