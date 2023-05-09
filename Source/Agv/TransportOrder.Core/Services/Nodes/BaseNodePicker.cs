using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes;

/// <summary>
/// Abstract class to implement when choosing nodes
/// </summary>
public abstract class BaseNodePicker
{
    /// <summary>
    /// Note State Registry
    /// </summary>
    protected readonly INodeStateRegistry NodeStateRegistry;

    /// <summary>
    /// The graph registry
    /// </summary>
    protected readonly IGraphRegistry GraphRegistry;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="graphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    protected BaseNodePicker(IGraphRegistry graphRegistry, INodeStateRegistry nodeStateRegistry)
    {
        GraphRegistry = graphRegistry;
        NodeStateRegistry = nodeStateRegistry;
    }

    /// <summary>
    /// For what nodes are you looking for? Used for throwing of exceptions
    /// </summary>
    protected abstract string Description { get; }

    private static AgvGraph ValidateAgvGraph(IAgv agv)
    {
        var agvGraph = agv.AgvGraph;
        if (agvGraph == null)
        {
            throw new AgvGraphNotDefinedException(agv);
        }
        return agvGraph;
    }

    /// <summary>
    /// The external method to get the actual node which has been selected by the logic 
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected AgvGraph.Node PickNode(IAgv agv)
    {
        IEnumerable<Node> availableNodes = GetAvailableNodes(agv);
        var node = SelectBestNode(availableNodes, agv);

        var agvNode = GetAgvNode(agv, node);
        return agvNode;
    }

    private IEnumerable<Node> GetAvailableNodes(IAgv agv)
    {
        IEnumerable<Node> availableNodes = NodeStateRegistry.FilterFreeUnlockedNodes(GraphRegistry.GetNodes(NodeFilter, new []{agv}));
        // ReSharper disable once PossibleMultipleEnumeration
        if (availableNodes == null || !availableNodes.Any())
        {
            throw new NoNodesAvailableException(Description);
        }
        // ReSharper disable once PossibleMultipleEnumeration
        return availableNodes;
    }
    
    /// <summary>
    /// Implement this method to return the nodes which fit your logic
    /// </summary>
    /// <returns></returns>
    protected abstract Expression<Func<Node, bool>> NodeFilter { get; }

    /// <summary>
    /// Logic for selection of best node. This method can be overriden, but it's base implementation does a random selection
    /// </summary>
    /// <param name="availableNodes">The nodes which can be selected, it is ensured that it contains at least one available node</param>
    /// <param name="agv"></param>
    /// <returns>The best node</returns>
    protected virtual Node SelectBestNode(IEnumerable<Node> availableNodes, IAgv agv)
    {
        var rand = Random.Shared;
        // must use ToList, otherwise it can not be interpreted
        List<Node> nodes = availableNodes.ToList(); 
        var node = nodes.ElementAt(rand.Next(nodes.Count));
        return node;
    }

    private static AgvGraph.Node GetAgvNode(IAgv agv, Node node)
    {
        var agvGraph = ValidateAgvGraph(agv);

        var agvNode = agvGraph.Nodes.FirstOrDefault(n => n.Id == node.Id);
        if (agvNode == null)
        {
            throw new NodeNotFoundException(agv, $"NodeId: {node.Id} not found in AgvGraph");
        }
        return agvNode;
    }

    /// <summary>
    /// Checks if the agv is on such a node defined by the <see cref="NodeFilter"/>
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>True if the lastNodeId of the Agv is such a node, False if the lastNodeId of the agv is null</returns>
    protected bool IsAgvOnSuchANode(IAgv agv)
    {
        string? lastNodeId = agv.AgvStatus?.LastNodeId;
        if (string.IsNullOrEmpty(lastNodeId))
        {
            return false;
        }
        var currentNode = GraphRegistry.GetNodes(n => n.Id == lastNodeId).SingleOrDefault();
        bool isSuchANode = NodeFilter.Compile()(currentNode!);

        return isSuchANode;
    }
}