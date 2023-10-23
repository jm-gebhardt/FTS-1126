using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Helpers;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;

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
public class GridGraphGenerator : IGraphGenerator
{
    /// <summary>
    /// GridGraphGenerator Constructor
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="destinations"></param>
    /// <param name="gridRows"></param>
    /// <param name="serviceNodes"></param>
    public GridGraphGenerator(
        string[] sources,
        string[] destinations,
        int gridRows = 3,
        AgvGraph.NodeType[]? serviceNodes = null)
    {
        Sources = sources;
        Destinations = destinations;
        _gridRows = gridRows;
        _serviceNodeTypes = serviceNodes ?? Array.Empty<AgvGraph.NodeType>();
    }

    /// <summary>
    /// List of source nodes
    /// </summary>
    public string[] Sources { get; }

    /// <summary>
    /// List of destination nodes
    /// </summary>
    public string[] Destinations { get; }

    private const float OriginXOffset = 5;
    private const float OriginYOffset = 5;
    private const float NodeOffset = 5;
    private const float EdgeMetric = 1;

    private int GridWidth => Math.Max(Destinations.Length, Sources.Length);

    private readonly List<GraphNode> _gridNodes = new();
    private readonly List<GraphNode> _otherNodes = new();
    private readonly List<GraphEdge> _edges = new();

    private readonly int _gridRows;
    private readonly AgvGraph.NodeType[] _serviceNodeTypes;

    private string RelatedMapName => $"SIM_{GridWidth}x{_gridRows}";

    /// <summary>
    /// Generate AgvGraph
    /// </summary>
    /// <returns></returns>
    public AgvGraph Generate(Guid correlationId)
    {
        Clear();
        AddGrid();
        AddSourceNodes();
        AddDestinationNodes();
        AddServiceNodes();

        return new AgvGraph(
            correlationId,
            _otherNodes.Concat(_gridNodes).ToArray(),
            _edges.ToArray(),
            RelatedMapName.ToGuid(),
            RelatedMapName
        );
    }

    /// <summary>
    /// Clear internal state
    /// </summary>
    private void Clear()
    {
        _gridNodes.Clear();
        _otherNodes.Clear();
        _edges.Clear();
    }

    private void AddGrid()
    {
        // Generate grid nodes
        for (int y = 0; y < _gridRows; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                _gridNodes.Add(new GraphNode($"N{x:000}-{y:000}")
                {
                    Type = AgvGraph.NodeType.WayPoint,
                    X = x * NodeOffset + OriginXOffset,
                    Y = y * NodeOffset + OriginYOffset
                });
            }
        }
        // Connect grid nodes
        for (int y = 0; y < _gridRows; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                // Connect horizontal
                if (x != GridWidth - 1)
                {
                    AddEdge(
                        _gridNodes[y * GridWidth + x],
                        _gridNodes[y * GridWidth + x + 1]
                    );
                }

                // Connect vertical
                if (y != _gridRows - 1)
                {
                    AddEdge(
                        _gridNodes[y * GridWidth + x],
                        _gridNodes[(y + 1) * GridWidth + x]
                    );
                }
            }
        }
    }

    /// <summary>
    /// Add sources nodes and edges
    /// </summary>
    private void AddSourceNodes()
    {
        for (int index = 0; index < Sources.Length; index++)
        {
            string nodeId = Sources[index];
            var gridNode = _gridNodes[index];
            var sourceNode = new GraphNode(nodeId)
            {
                Type = AgvGraph.NodeType.Pick,
                X = gridNode.X,
                Y = gridNode.Y - OriginYOffset / 2
            };
            _otherNodes.Add(sourceNode);
            AddEdge(sourceNode, gridNode);
        }
    }

    /// <summary>
    /// Add destination nodes and edges
    /// </summary>
    private void AddDestinationNodes()
    {
        for (int index = 0; index < Destinations.Length; index++)
        {
            string nodeId = Destinations[Destinations.Length - index - 1];
            var gridNode = _gridNodes[_gridNodes.Count - index - 1];
            var destinationNode = new GraphNode(nodeId)
            {
                Type = AgvGraph.NodeType.Drop,
                X = gridNode.X,
                Y = gridNode.Y + OriginYOffset / 2
            };
            _otherNodes.Add(destinationNode);
            AddEdge(destinationNode, gridNode);
        }
    }

    /// <summary>
    /// Add services nodes and edges using <see cref="AddServiceNode"/>
    /// </summary>
    private void AddServiceNodes()
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
        var gridNode = _gridNodes[connectedGridNodeIdx];
        string nodeId = $"{Enum.GetName(nodeType)}-{index}";

        var serviceNode = new GraphNode(nodeId)
        {
            Type = nodeType,
            X = gridNode.X + OriginXOffset / 2 * side,
            Y = gridNode.Y
        };
        _otherNodes.Add(serviceNode);
        AddEdge(serviceNode, gridNode);
    }

    private int FindConnectedGridNodeIndex(int serviceNodeIndex, out int side)
    {
        if (_serviceNodeTypes.Length == 0)
        {
            throw new IndexOutOfRangeException("Number of service nodes need to be greater than 0.");
        }
        if (_serviceNodeTypes.Length > 2 * _gridRows)
        {
            throw new IndexOutOfRangeException("Grid is to small to add service nodes.");
        }

        float step = 2.0f * _gridRows / _serviceNodeTypes.Length;
        int nodeIdx = (int)(step * serviceNodeIndex + 0.5);

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
    private void AddEdge(GraphNode fromNode, GraphNode toNode, bool bidirectional = true)
    {
        string fromNodeId = fromNode.Id;
        string toNodeId = toNode.Id;
        _edges.Add(new GraphEdge(fromNodeId, toNodeId, EdgeMetric, GraphHelper.ToGuid(fromNodeId, EdgeMetric, toNodeId)));
        if (bidirectional)
        {
            _edges.Add(new GraphEdge(toNodeId, fromNodeId, EdgeMetric, GraphHelper.ToGuid(toNodeId, EdgeMetric, fromNodeId)));
        }
    }
}