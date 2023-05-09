using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs
{
    /// <summary>
    /// Graph registry using entity framework for AgvGraphs
    /// </summary>
    public class EfGraphRegistry : IGraphRegistry
    {
        private static readonly object _dbLockObject = new();

        private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
        private readonly ILogger<EfGraphRegistry> _logger;

        /// <summary>
        /// Constructor of the EF MapRegistry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="logger"></param>
        public EfGraphRegistry(
            IDbContextFactory<TransportOrderDbContext> dbContextFactory,
            ILogger<EfGraphRegistry> logger
            )
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Graph GetGraph()
        {
            lock (_dbLockObject)
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                Node[] nodes = dbContext.Set<Node>().AsNoTracking().ToArray();
                Edge[] edges = dbContext.Set<Edge>().AsNoTracking().ToArray();
                return new Graph(nodes, edges);
            }
        }

        /// <inheritdoc/>
        public IQueryable<Node> GetNodes(Expression<Func<Node, bool>>? predicate = null, IEnumerable<IAgv>? agvs = null)
        {
            lock (_dbLockObject)
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                IQueryable<Node> nodes = dbContext.Set<Node>().AsNoTracking();

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
                IQueryable<Edge> edges = dbContext.Set<Edge>().AsNoTracking();

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

                    DbSet<Node> nodes = dbContext.Set<Node>();
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

                    DbSet<Edge> edges = dbContext.Set<Edge>();
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
                    _logger.LogError(e, "Could not add or update graph");
                    return false;
                }
            }
        }
    }
}