using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Read model describing an AGV type together with its computed graph availability.
/// </summary>
/// <param name="Id">The unique identifier of the AGV type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the AGV type, consisting of manufacturer and series name.</param>
/// <param name="Manufacturer">The manufacturer of the AGV type.</param>
/// <param name="SeriesName">The series name of the AGV type.</param>
/// <param name="Length">The length of the AGV type in [m].</param>
/// <param name="Width">The width of the AGV type in [m].</param>
/// <param name="Acknowledged">Whether the AGV type has been acknowledged by an operator.</param>
/// <param name="ReportedByAgv">Whether the AGV type was reported by an AGV itself,</param>
/// <param name="HasGraph">Whether at least one AGV of this type has a graph.</param>
/// <param name="RawData">The raw data of the type.</param>
[PublicAPI]
public record AgvTypeReadModel(
    Guid Id,
    string AgvTypeIdentifier,
    string Manufacturer,
    string SeriesName,
    double Length,
    double Width,
    bool Acknowledged,
    bool ReportedByAgv,
    bool HasGraph,
    string RawData);