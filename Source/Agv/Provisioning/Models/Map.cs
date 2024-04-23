using JetBrains.Annotations;

namespace Gebhardt.Acc.Provisioning.Models;

/// <summary>
/// Base data of the map.
/// </summary>
[UsedImplicitly]
public class Map
{
    /// <summary>
    /// The CorrelationId of the map.
    /// </summary>
    public Guid CorrelationId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The human readable unique MapId / Name.
    /// </summary>
    public string Name
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <inheritdoc cref="Map"/>
    public Map(Guid correlationId, string name)
    {
        CorrelationId = correlationId;
        Name = name;
    }
}