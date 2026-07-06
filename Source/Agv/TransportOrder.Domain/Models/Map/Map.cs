using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map;

/// <summary>
/// class representing the Map table in Database
/// </summary>
public record Map
{
    /// <summary>
    /// The correlation id o the map.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The name of the map.
    /// </summary>
    public string MapName { get; init; }

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
    /// Stable identity for the single default map instance, derived from the reserved name "default".
    /// </summary>
    public static readonly Guid DefaultMapId = DefaultMapName.ToGuid();
    
    private static string DefaultMapName => "default";
    
    /// <summary>
    /// Create a default map seed (used only at first-time initialization).
    /// </summary>
    public static Map CreateDefaultMap()
    {
        List<GraphNode> nodes = [new() { X = 0, Y = 0 }, new() { X = 1, Y = 1 }];
        var generator = new GeneratedMapGenerator(TimeProvider.System);
        var map = generator.Generate(DefaultMapId, DefaultMapName, new Common.Domain.Models.Graphs.Graph { Nodes = nodes });
        byte[] mapValue = Convert.FromBase64String(map.ImageData);
        return new Map(DefaultMapId, DefaultMapName, new MapBackground(Guid.NewGuid(), map.Resolution, map.XOffset, map.YOffset, map.Width, map.Height, mapValue), DateTime.UtcNow)
        {
            Nodes = [],
            Edges = [],
            MapAgvs = []
        };
    }

    /// <summary>
    /// The list of nodes in the graph.
    /// </summary>
    public required ICollection<GraphNode> Nodes { get; init; }

    /// <summary>
    /// The list of edges in the graph.
    /// </summary>
    public required ICollection<GraphEdge> Edges { get; init; }

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
        MapName = null!;
    }

    /// <inheritdoc cref="Map"/>
    public Map(Guid correlationId, string mapName, MapBackground? baseBackground, DateTime modificationDate)
    {
        CorrelationId = correlationId;
        MapName = mapName;
        BaseBackground = baseBackground;
        ModificationDate = modificationDate;
        Nodes = new List<GraphNode>();
        Edges = new List<GraphEdge>();
        MapAgvs = new List<MapAgv>();
    }
}