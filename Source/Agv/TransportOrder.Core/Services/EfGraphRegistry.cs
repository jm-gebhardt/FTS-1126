using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Numerics;
using NodeState = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvGraph.NodeState;
using NodeType = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvGraph.NodeType;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Map Registry using Entity Framework to access the AgvSaga persistence storage
    /// </summary>
    public class EfGraphRegistry : IGraphRegistry
    {
        private static readonly object _dbLockObject = new();

        private readonly IDbContextFactory<SagaPersistenceDbContext> _dbContextFactory;
        private readonly ILogger<EfGraphRegistry> _logger;
        private readonly IAgvSettingsProvider _agvSettingsProvider;

        private readonly ConcurrentDictionary<Guid, Node?> _agv2Node = new(); // Assigns an AGV to a node to track occupied nodes

        /// <summary>
        /// Constructor of the EF MapRegistry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="logger"></param>
        /// <param name="agvSettingsProvider"></param>
        public EfGraphRegistry(
            IDbContextFactory<SagaPersistenceDbContext> dbContextFactory,
            ILogger<EfGraphRegistry> logger,
            IAgvSettingsProvider agvSettingsProvider)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _agvSettingsProvider = agvSettingsProvider;
        }

        /// <inheritdoc/>
        public Graph GetGraph()
        {
            lock (_dbLockObject)
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                Node[] nodes = dbContext.Nodes.AsNoTracking().ToArray();
                Edge[] edges = dbContext.Edges.AsNoTracking().ToArray();
                return new Graph(nodes, edges);
            }
        }

        /// <inheritdoc/>
        public IQueryable<Node> GetNodes(Expression<Func<Node, bool>>? predicate = null)
        {
            lock (_dbLockObject)
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                IQueryable<Node> nodes = dbContext.Nodes.AsNoTracking();

                if (predicate != null)
                {
                    return nodes.Where(predicate);
                }
                return nodes;
            }
        }

        /// <inheritdoc/>
        public IQueryable<Edge> GetEdges(Expression<Func<Edge, bool>>? predicate = null)
        {
            lock (_dbLockObject)
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                IQueryable<Edge> edges = dbContext.Edges.AsNoTracking();

                if (predicate != null)
                {
                    return edges.Where(predicate);
                }
                return edges;
            }
        }

        /// <inheritdoc/>
        public bool AddOrUpdateNode(Node node)
        {
            var graph = new Graph(new[] { node }, new Edge[] { });
            return AddOrUpdateGraph(graph);
        }

        /// <inheritdoc/>
        public bool AddOrUpdateEdge(Edge edge)
        {
            var graph = new Graph(new Node[] { }, new[] { edge });
            return AddOrUpdateGraph(graph);
        }

        /// <inheritdoc/>
        public bool AddOrUpdateGraph(AgvGraph agvGraph)
        {
            var nodes = new List<Node>();
            foreach (var node in agvGraph.Nodes)
            {
                nodes.Add(new()
                {
                    Id = node.Id,
                    Type = node.Type,
                    X = node.X,
                    Y = node.Y,
                    Z = node.Z,
                    Theta = node.Theta
                });
            }

            var edges = new List<Edge>();
            foreach (var edge in agvGraph.Edges)
            {
                edges.Add(new()
                {
                    FromId = edge.From,
                    ToId = edge.To,
                    Metric = edge.Metric,
                    IsBlocked = edge.IsBlocked
                });
            }

            var graph = new Graph(nodes.ToArray(), edges.ToArray());
            return AddOrUpdateGraph(graph);
        }

        /// <inheritdoc/>
        public bool AddOrUpdateGraph(Graph graph)
        {
            if (graph.Nodes.Length == 0 && graph.Edges.Length == 0) // Check if there is actually something to update
            {
                return true;
            }

            lock (_dbLockObject)
            {
                try
                {
                    var dbContext = _dbContextFactory.CreateDbContext();

                    DbSet<Node> nodes = dbContext.Nodes;
                    foreach (var node in graph.Nodes)
                    {
                        if (nodes.Contains(node))
                        {
                            nodes.Update(node);
                        }
                        else
                        {
                            nodes.Add(node);
                        }
                    }

                    DbSet<Edge> edges = dbContext.Edges;
                    foreach (var edge in graph.Edges)
                    {
                        if (edges.Contains(edge))
                        {
                            edges.Update(edge);
                        }
                        else
                        {
                            edges.Add(edge);
                        }
                    }

                    return dbContext.SaveChanges() != 0;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not add or update graph.");
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public bool LockNode(string nodeId)
        {
            lock (_dbLockObject)
            {
                try
                {
                    var dbContext = _dbContextFactory.CreateDbContext();

                    IQueryable<Node> nodes = dbContext.Nodes
                        .Where(n => n.Id == nodeId);

                    if (!nodes.Any())
                    {
                        _logger.LogWarning("Node {NodeId} not found.", nodeId);
                        return false;
                    }

                    nodes.ForEachAsync(node => node.State = NodeState.Locked)
                        .Wait();

                    dbContext.SaveChanges();
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not update node.");
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public bool FreeNode(string nodeId)
        {
            lock (_dbLockObject)
            {
                try
                {
                    var dbContext = _dbContextFactory.CreateDbContext();

                    IQueryable<Node> nodes = dbContext.Nodes
                        .Where(n => n.Id == nodeId);

                    if (!nodes.Any())
                    {
                        _logger.LogWarning("Node {NodeId} not found.", nodeId);
                        return false;
                    }

                    nodes.ForEachAsync(node => node.State = NodeState.Free)
                        .Wait();

                    dbContext.SaveChanges();
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not update node.");
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public bool ReserveNode(string nodeId)
        {
            lock (_dbLockObject)
            {
                try
                {
                    var dbContext = _dbContextFactory.CreateDbContext();

                    dbContext.Nodes
                        .Where(node => node.Id == nodeId && node.State == NodeState.Free)
                        .ForEachAsync(node => node.State = NodeState.Reserved)
                        .Wait();

                    return dbContext.SaveChanges() != 0; // No change = no free node found
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not update node.");
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public bool UpdateOccupiedNode(
            AgvPositionStatus agvPositionStatus,
            out Node? previousOccupiedNode,
            out Node? currentOccupiedNode)
        {
            previousOccupiedNode = null;
            currentOccupiedNode = null;
            List<Node> nodes = GetNodes(node =>
                node.Type == NodeType.Park ||
                node.Type == NodeType.Charge
            ).ToList();

            var agvPosition = agvPositionStatus.AgvPosition;
            float occupiedDistance = GetAgvOccupiedDistance(agvPositionStatus.CorrelationId);

            // Get node that was last occupied by this AGV
            if (!_agv2Node.TryGetValue(agvPositionStatus.CorrelationId, out previousOccupiedNode))
            {
                previousOccupiedNode = null;
            }

            if (previousOccupiedNode != null)
            {
                // Check if same node is still occupied
                if (CheckOccupiedDistance(agvPosition, previousOccupiedNode, occupiedDistance))
                {
                    currentOccupiedNode = previousOccupiedNode;
                    return true; // Same node, no changes
                }

                previousOccupiedNode.State = NodeState.Free; // Node is no longer occupied by this AGV
                if (!AddOrUpdateNode(previousOccupiedNode)) // Update state of old node
                {
                    return false; // An error occurred
                }

                // Find new occupied node
                currentOccupiedNode = FindOccupiedNode(nodes, agvPosition, occupiedDistance);
            }
            else
            {
                currentOccupiedNode = FindOccupiedNode(nodes, agvPosition, occupiedDistance);
            }

            // Update state of new node if it is not locked
            if (currentOccupiedNode != null && currentOccupiedNode.State != NodeState.Locked)
            {
                currentOccupiedNode.State = NodeState.Occupied;
                if (!AddOrUpdateNode(currentOccupiedNode))
                {
                    return false; // An error occurred
                }
            }

            var temp = currentOccupiedNode; // Temp var because out variables can't be used in lambda functions
            _agv2Node.AddOrUpdate(agvPositionStatus.CorrelationId, currentOccupiedNode, (_, _) => temp);

            return true;
        }

        /// <inheritdoc/>
        public bool SetAllOccupiedNodes(List<AgvPositionStatus> allAgvPositions)
        {
            lock (_dbLockObject)
            {
                try
                {
                    var dbContext = _dbContextFactory.CreateDbContext();

                    // Set occupied nodes based on positions of all AGVs
                    dbContext.Nodes
                        .Where(node =>
                            node.State == NodeState.Free ||
                            node.State == NodeState.Reserved ||
                            node.State == NodeState.Occupied
                        )
                        .ForEachAsync(node =>
                        {
                            var state = node.State == NodeState.Reserved ? NodeState.Reserved : NodeState.Free; // Reserved nodes stay reserved
                            foreach (var agvPosition in allAgvPositions)
                            {
                                float occupiedDistance = GetAgvOccupiedDistance(agvPosition.CorrelationId);

                                if (CheckOccupiedDistance(agvPosition.AgvPosition, node, occupiedDistance))
                                {
                                    state = NodeState.Occupied;
                                    break;
                                }
                            }
                            node.State = state;
                        }).Wait();

                    dbContext.SaveChanges();
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not add or update graph.");
                    return false;
                }
            }
        }

        #region Utils

        /// <summary>
        /// Checks if an AGV occupies a node based on the position of the AGV
        /// </summary>
        /// <param name="nodes">Nodes collection</param>
        /// <param name="agvPosition">Position of AGV</param>
        /// <param name="occupiedDistance">Min. distance between an AGV and a node for a node to be considered 'Occupied'</param>
        /// <returns>The node that is occupied by the AGV or null if no node is occupied</returns>
        private Node? FindOccupiedNode(IEnumerable<Node> nodes, AgvPosition agvPosition, float occupiedDistance)
        {
            Node? occupiedNode = null;

            // Then check distance between each node to find out which node is occupied
            foreach (var node in nodes)
            {
                if (CheckOccupiedDistance(agvPosition, node, occupiedDistance))
                {
                    occupiedNode = node;
                    break; // Stop after first occupied node (AGV can only occupy one node, might change in the future)
                }
            }

            return occupiedNode;
        }

        /// <summary>
        /// Checks if an AGV is close enough to occupy a node
        /// </summary>
        /// <returns>True if the AGV is close enough to occupy the node</returns>
        private bool CheckOccupiedDistance(AgvPosition agvPosition, Node node, float occupiedDistance)
        {
            var posAgv = new Vector2(agvPosition.X, agvPosition.Y);
            var posNode = new Vector2(node.X, node.Y);
            float distance = Vector2.Distance(posAgv, posNode);
            return distance <= occupiedDistance;
        }

        /// <summary>
        /// Returns the occupy distance for a given AGV
        /// </summary>
        /// <param name="agvCorrelationId"></param>
        /// <returns></returns>
        private float GetAgvOccupiedDistance(Guid agvCorrelationId)
        {
            var settings = _agvSettingsProvider.GetSettingsForAgv(agvCorrelationId);
            return settings.OccupiedDistanceThreshold;
        }

        #endregion
    }
}