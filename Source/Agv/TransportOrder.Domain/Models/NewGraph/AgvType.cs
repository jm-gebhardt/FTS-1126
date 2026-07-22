using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents a type of AGV in the system. This class encapsulates unique AgvTypeIdentifier and properties, which are derived from the manufacturer's factsheet.
/// </summary>
[UsedImplicitly]
public class AgvType
{
    /// <summary>
    /// Unique internal identifier for the AGV type.
    /// </summary>
    [UsedImplicitly]
    public Guid Id { get; }

    /// <summary>
    /// Identifier for the AGV type. Usually equals [manufacturer].[seriesName] from factsheet.
    /// </summary>
    [UsedImplicitly]
    public string AgvTypeIdentifier { get; }

    //other properties derived from Factsheet

    /// <inheritdoc cref="AgvType"/>
    public AgvType(Guid id, string agvTypeIdentifier)
    {
        Id = id;
        AgvTypeIdentifier = agvTypeIdentifier;
    }
}