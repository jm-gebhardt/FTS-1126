using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.OpenApi.Extensions;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph
{
    /// <summary>
    /// Generated Grid Style Graph
    /// Source will be connected in the north, services to left and right, and destinations to the south.
    /// The grid dimensions are defined by gridLength (Default: 3) and by the number of Sources and destinations.
    /// Grid length must be large enough to connect the given service nodes.
    /// 
    ///           S0   S1  S2
    ///            |   |   |
    /// Service0 - * - * - * - Service1
    /// Service2 - * - * - *
    ///            * - * - *
    ///            |   |   |
    ///           D0  D1  D2
    /// 
    /// </summary>
    public class GridGraphGenerator: IGraphGenerator
    {
        /// <summary>
        /// GridGraphGenerator Constructor
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="destinations"></param>
        /// <param name="gridLength"></param>
        /// <param name="serviceNodes"></param>
        public GridGraphGenerator(
            string[] sources, 
            string[] destinations, 
            int gridLength = 3,
            AgvGraph.NodeType[]? serviceNodes = null)
        {
            _gridLength = gridLength;
            _serviceNodeTypes = serviceNodes ?? Array.Empty<AgvGraph.NodeType>();
            Sources = sources;
            Destinations = destinations;
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

        private readonly List<AgvGraph.Node> _gridNodes = new();
        private readonly List<AgvGraph.Node> _otherNodes = new();
        private readonly List<AgvGraph.Edge> _edges = new();
    
        private readonly int _gridLength;
        private readonly AgvGraph.NodeType[] _serviceNodeTypes;

        private string RelatedMapName => $"SIM_{GridWidth}x{_gridLength}";

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
            for (int y = 0; y < _gridLength; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    _gridNodes.Add(new AgvGraph.Node($"N{y:0000}-{x:0000}")
                    {
                        Type = AgvGraph.NodeType.WayPoint,
                        X = x * NodeOffset + OriginXOffset,
                        Y = y * NodeOffset + OriginYOffset
                    });
                }
            }
            // Connect grid nodes
            for (int y = 0; y < _gridLength; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    // Connect horizontal
                    if (x != GridWidth - 1)
                    {
                    
                        AddEdge(
                            _gridNodes[y * GridWidth + x].Id,
                            _gridNodes[y * GridWidth + x + 1].Id
                        );
                    }

                    // Connect vertical
                    if (y != _gridLength - 1)
                    {
                        AddEdge(
                            _gridNodes[y * GridWidth + x].Id,
                            _gridNodes[(y + 1) * GridWidth + x].Id
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
                _otherNodes.Add(new AgvGraph.Node(nodeId)
                {
                    Type = AgvGraph.NodeType.Station,
                    X = gridNode.X,
                    Y = gridNode.Y - OriginYOffset / 2
                });
                AddEdge(nodeId, gridNode.Id);
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
                _otherNodes.Add(new AgvGraph.Node(nodeId)
                {
                    Type = AgvGraph.NodeType.Station,
                    X = gridNode.X,
                    Y = gridNode.Y + OriginYOffset / 2
                });
                AddEdge(nodeId, gridNode.Id);
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
            int connectedGridNodeIdx = FindConnectedGridNodeIndex(index);
            var gridNode = _gridNodes[connectedGridNodeIdx];
            string nodeId = $"{nodeType.GetDisplayName()}-{index}";
        
            _otherNodes.Add(new AgvGraph.Node(nodeId)
            {
                Type = nodeType,
                X = gridNode.X + OriginXOffset /  2 * (index % 2 == 0 ? -1 : 1),
                Y = gridNode.Y
            });   
            AddEdge(nodeId, gridNode.Id);
        }

        private int FindConnectedGridNodeIndex(int serviceNodeIndex)
        {
            if (serviceNodeIndex == 0)
            {
                return 0;
            }
            // Select grid node left, right, left, right, ...
            int connectedGridNodeIdx = serviceNodeIndex % 2 == 0 ? 
                GridWidth * (serviceNodeIndex - 1) :  
                GridWidth * (int)Math.Ceiling(serviceNodeIndex / 2.0) - 1;
            if (connectedGridNodeIdx >= _gridNodes.Count)
            {
                throw new IndexOutOfRangeException($"GridLength is to small to add service node with index {serviceNodeIndex}");
            }

            return connectedGridNodeIdx;
        }

        /// <summary>
        /// Add Edge
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="bidirectional"></param>
        private void AddEdge(string from, string to, bool bidirectional = true)
        {
            _edges.Add(new AgvGraph.Edge(from, to) {Metric = EdgeMetric});
            if (bidirectional)
            {
                _edges.Add(new AgvGraph.Edge(to, from){Metric = EdgeMetric});
            }
        }
    }
}