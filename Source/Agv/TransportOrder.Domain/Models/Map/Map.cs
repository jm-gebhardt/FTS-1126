using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map;

/// <summary>
/// class representing the Map table in Database
/// </summary>
public record Map
{
    /// <summary>
    /// The correlation id of the map.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// MapId
    /// </summary>
    public string MapId { get; init; }

    /// <summary>
    /// Timestamp of last modification
    /// </summary>
    public DateTime ModificationDate { get; init; }

    /// <summary>
    /// The identifier of the base map background.
    /// </summary>
    [UsedImplicitly]
    public Guid? BaseBackgroundId => BaseBackground?.CorrelationId;

    /// <summary>
    /// The base background image of the map.
    /// </summary>
    public MapBackground? BaseBackground
    {
        get;
        set
        {
            field = value;
            if (field != null)
            {
                field.IsCustom = false;
            }
        }
    }

    /// <summary>
    /// The Identifier of the custom map background.
    /// </summary>
    [UsedImplicitly]
    public Guid? CustomBackgroundId => CustomBackground?.CorrelationId;

    /// <summary>
    /// Optional custom background image of the map.
    /// </summary>
    public MapBackground? CustomBackground
    {
        get;
        set
        {
            field = value;
            if (field != null)
            {
                field.IsCustom = true;
            }
        }
    }

    /// <summary>
    /// The list of nodes in the graph.
    /// </summary>
    public required ICollection<EfNode> Nodes { get; init; }

    /// <summary>
    /// The list of edges in the graph.
    /// </summary>
    public required ICollection<EfEdge> Edges { get; init; }

    /// <summary>
    /// The list of AgvIds related to this map.
    /// </summary>
    [UsedImplicitly]
    public required ICollection<MapAgv> MapAgvs { get; init; }

    /// <summary>
    /// Internal
    /// </summary>
    private Map()
    {
        MapId = null!;
    }

    /// <inheritdoc cref="Map"/>
    public Map(Guid correlationId, string mapId, MapBackground? baseBackground, DateTime modificationDate)
    {
        CorrelationId = correlationId;
        MapId = mapId;
        BaseBackground = baseBackground;
        ModificationDate = modificationDate;
        Nodes = new List<EfNode>();
        Edges = new List<EfEdge>();
        MapAgvs = new List<MapAgv>();
    }
}