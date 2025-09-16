using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Generated Grid Style Graph
/// Source will be connected in the north, services to left and right, and destinations to the south.
/// The grid dimensions are defined by gridRows (Default: 3) and by the number of Sources and destinations.
/// Grid rows must be large enough to connect the given service nodes.
/// 
///           S0   S1  S2
///            |   |   |
/// Service0 - * - * - *
///            * - * - * - Service1
/// Service2 - * - * - *
///            |   |   |
///           D0  D1  D2
/// 
/// </summary>
public class GridGraphGenerator : GraphGeneratorBase
{
    /// Station angles are defined as:
    ///         0
    /// 3/2 pi -|- pi/2
    ///         pi
    private const float East = (float) Math.PI / 2f;

    private const float West = 3f * (float) Math.PI / 2f;
    private const float South = (float) Math.PI;
    private const float North = 0f;

    private const float OriginXOffset = 5;
    private const float OriginYOffset = 5;
    private const float NodeOffset = 5;
    private const float EdgeMetric = 1;
    private readonly DateTime _timeStamp = DateTime.UtcNow;

    /// <summary>
    /// The width of the grid.
    /// </summary>
    protected int GridWidth => Math.Max(Destinations.Length, Sources.Length);

    /// <summary>
    /// The grid nodes.
    /// </summary>
    protected readonly List<GraphNode> GridNodes = new();

    /// <summary>
    /// Other nodes in the graph (e.g. service nodes).
    /// </summary>
    protected readonly List<GraphNode> OtherNodes = new();

    /// <summary>
    /// Edges of the graph.
    /// </summary>
    protected readonly List<GraphEdge> Edges = new();

    /// <summary>
    /// The number of grid rows.
    /// </summary>
    protected readonly int GridRows;

    private readonly AgvGraph.NodeType[] _serviceNodeTypes;

    /// <inheritdoc />
    protected override Guid GraphId => $"SIM_{GridWidth}x{GridRows}".ToGuid();

    /// <summary>
    /// Initializes a new <see cref="GridGraphGenerator"/> instance based on simulation options.
    /// </summary>
    public GridGraphGenerator(Options options, IMapGenerator mapGenerator)
        : base(options.Sources != null ? options.Sources.ToArray() : Array.Empty<string>(),
            options.Destinations != null ? options.Destinations.ToArray() : Array.Empty<string>(),
            mapGenerator)
    {
        GridRows = options.GridRows;
        _serviceNodeTypes = CreateServiceNodes();
    }

    /// <summary>
    /// GridGraphGenerator Constructor
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="destinations"></param>
    /// <param name="mapGenerator"></param>
    /// <param name="gridRows"></param>
    /// <param name="serviceNodes"></param>
    public GridGraphGenerator(
        string[] sources,
        string[] destinations,
        IMapGenerator mapGenerator,
        int gridRows = 3,
        AgvGraph.NodeType[]? serviceNodes = null)
        : base(sources, destinations, mapGenerator)
    {
        GridRows = gridRows;
        _serviceNodeTypes = serviceNodes ?? Array.Empty<AgvGraph.NodeType>();
    }

    /// <summary>
    /// Generate AgvGraph
    /// </summary>
    /// <returns></returns>
    public override AgvGraph Generate(Guid correlationId)
    {
        Clear();
        AddGrid();
        AddSourceNodes();
        AddDestinationNodes();
        AddServiceNodes();

        return new AgvGraph(
            correlationId,
            OtherNodes.Concat(GridNodes).ToArray(),
            Edges.ToArray()
        );
    }

    /// <summary>
    /// Clear internal state
    /// </summary>
    protected void Clear()
    {
        GridNodes.Clear();
        OtherNodes.Clear();
        Edges.Clear();
    }

    /// <summary>
    /// Add grid.
    /// </summary>
    protected void AddGrid()
    {
        // Generate grid nodes
        for (int y = 0; y < GridRows; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                string name = $"N{x:000}-{y:000}";
                GridNodes.Add(new GraphNode(name.ToGuid(), name)
                {
                    Type = AgvGraph.NodeType.WayPoint,
                    X = x * NodeOffset + OriginXOffset,
                    Y = y * NodeOffset + OriginYOffset,
                    TimeStamp = _timeStamp
                });
            }
        }
        // Connect grid nodes
        for (int y = 0; y < GridRows; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                // Connect horizontal
                if (x != GridWidth - 1)
                {
                    AddEdge(
                        GridNodes[y * GridWidth + x],
                        GridNodes[y * GridWidth + x + 1]
                    );
                }

                // Connect vertical
                if (y != GridRows - 1)
                {
                    AddEdge(
                        GridNodes[y * GridWidth + x],
                        GridNodes[(y + 1) * GridWidth + x]
                    );
                }
            }
        }
    }

    /// <summary>
    /// Add sources nodes and edges
    /// </summary>
    protected void AddSourceNodes()
    {
        if (GridNodes.Count == 0)
        {
            return;
        }

        for (int index = 0; index < Sources.Length; index++)
        {
            string name = Sources[index];
            var gridNode = GridNodes[index];
            var sourceNode = new GraphNode(name.ToGuid(), name)
            {
                Type = AgvGraph.NodeType.Pick,
                X = gridNode.X,
                Y = gridNode.Y - OriginYOffset / 2,
                Theta = North,
                TimeStamp = _timeStamp
            };
            OtherNodes.Add(sourceNode);
            AddEdge(sourceNode, gridNode);
        }
    }

    /// <summary>
    /// Add destination nodes and edges
    /// </summary>
    protected void AddDestinationNodes()
    {
        if (GridNodes.Count == 0)
        {
            return;
        }

        for (int index = 0; index < Destinations.Length; index++)
        {
            string name = Destinations[Destinations.Length - index - 1];
            var gridNode = GridNodes[GridNodes.Count - index - 1];
            var destinationNode = new GraphNode(name.ToGuid(), name)
            {
                Type = AgvGraph.NodeType.Drop,
                X = gridNode.X,
                Y = gridNode.Y + OriginYOffset / 2,
                Theta = South,
                TimeStamp = _timeStamp
            };
            OtherNodes.Add(destinationNode);
            AddEdge(destinationNode, gridNode);
        }
    }

    /// <summary>
    /// Add services nodes and edges using <see cref="AddServiceNode"/>
    /// </summary>
    protected void AddServiceNodes()
    {
        for (int index = 0; index < _serviceNodeTypes.Length; index++)
        {
            AddServiceNode(_serviceNodeTypes[index], index);
        }
    }

    /// <summary>
    /// Add service node to the side of the node grid.
    /// Nodes are place to the left and right side of the grid.
    /// index must be &#60; GridLength x 2
    /// </summary>
    /// <param name="nodeType"></param>
    /// <param name="index"></param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    private void AddServiceNode(AgvGraph.NodeType nodeType, int index)
    {
        int connectedGridNodeIdx = FindConnectedGridNodeIndex(index, out int side);
        var gridNode = GridNodes[connectedGridNodeIdx];
        string name = $"{Enum.GetName(nodeType)}-{index}";

        var serviceNode = new GraphNode(name.ToGuid(), name)
        {
            Type = nodeType,
            X = gridNode.X + OriginXOffset / 2 * side,
            Y = gridNode.Y,
            Theta = side == -1 ? West : East,
            TimeStamp = _timeStamp
        };
        OtherNodes.Add(serviceNode);
        AddEdge(serviceNode, gridNode);
    }

    private int FindConnectedGridNodeIndex(int serviceNodeIndex, out int side)
    {
        if (_serviceNodeTypes.Length == 0)
        {
            throw new IndexOutOfRangeException("Number of service nodes must be greater than 0.");
        }
        if (_serviceNodeTypes.Length > 2 * GridRows)
        {
            throw new IndexOutOfRangeException("Grid is too small to add service nodes.");
        }

        float step = 2.0f * GridRows / _serviceNodeTypes.Length;
        int nodeIdx = (int) (step * serviceNodeIndex + 0.5);

        if (nodeIdx % 2 == 0)
        {
            // Place to the left
            side = -1;
            return nodeIdx / 2 * GridWidth;
        }
        // Place to the right
        side = 1;
        return nodeIdx / 2 * GridWidth + GridWidth - 1;
    }

    /// <summary>
    /// Add Edge
    /// </summary>
    /// <param name="fromNode"></param>
    /// <param name="toNode"></param>
    /// <param name="bidirectional"></param>
    protected virtual void AddEdge(GraphNode fromNode, GraphNode toNode, bool bidirectional = true)
    {
        var fromNodeId = fromNode.Id;
        var toNodeId = toNode.Id;
        Edges.Add(GraphEdge.CreateDirect(fromNodeId, toNodeId, EdgeMetric, GraphHelper.ToGuid(EdgeType.Direct, fromNode.Name, EdgeMetric, toNode.Name), _timeStamp));
        if (bidirectional)
        {
            Edges.Add(GraphEdge.CreateDirect(toNodeId, fromNodeId, EdgeMetric, GraphHelper.ToGuid(EdgeType.Direct, toNode.Name, EdgeMetric, fromNode.Name), _timeStamp));
        }
    }

    /// <summary>
    /// Create a service node for each side of the grid
    /// Park on left, charge on right
    /// </summary>
    /// <returns></returns>
    private AgvGraph.NodeType[] CreateServiceNodes()
    {
        int serviceNodeCount = GridRows * 2;
        var serviceNodes = new AgvGraph.NodeType[serviceNodeCount];
        for (int idx = 0; idx < serviceNodeCount; idx++)
        {
            serviceNodes[idx] = idx % 2 == 0 ? AgvGraph.NodeType.Park : AgvGraph.NodeType.Charge;
        }
        return serviceNodes;
    }
}