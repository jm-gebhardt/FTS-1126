using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes;

/// <summary>
/// Abstract class to implement when choosing nodes
/// </summary>
public abstract class BaseNodePicker
{
    /// <summary>
    /// The graph registry
    /// </summary>
    private readonly IAgvGraphRegistry _agvGraphRegistry;
    
    /// <summary>
    /// Note State Registry
    /// </summary>
    protected readonly INodeStateRegistry NodeStateRegistry;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="agvGraphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    protected BaseNodePicker(IAgvGraphRegistry agvGraphRegistry, INodeStateRegistry nodeStateRegistry)
    {
        _agvGraphRegistry = agvGraphRegistry;
        NodeStateRegistry = nodeStateRegistry;
    }

    /// <summary>
    /// For what nodes are you looking for? Used for throwing of exceptions
    /// </summary>
    protected abstract string Description { get; }
    
    /// <summary>
    /// Get a node from the registry
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected Task<GraphNode> GetNodeAsync(string nodeId, IAgv agv) => _agvGraphRegistry.GetNodeAsync(nodeId, agv.CorrelationId);

    /// <summary>
    /// The external method to get the actual node which has been selected by the logic 
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected async Task<GraphNode> PickNode(IAgv agv)
    {
        IEnumerable<GraphNode> availableNodes = await GetAvailableNodes(agv);
        var node = await SelectBestNode(availableNodes, agv);

        var agvNode = await GetAgvNode(agv, node);
        return agvNode;
    }

    private async Task<IEnumerable<GraphNode>> GetAvailableNodes(IAgv agv)
    {
        IEnumerable<GraphNode> nodes = await _agvGraphRegistry.GetNodesAsync(agv.CorrelationId, NodeFilter);
        IEnumerable<GraphNode> availableNodes = NodeStateRegistry.FilterFreeUnlockedNodes(nodes);
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
    protected abstract Expression<Func<GraphNode, bool>> NodeFilter { get; }

    /// <summary>
    /// Logic for selection of best node. This method can be overriden, but it's base implementation does a random selection
    /// </summary>
    /// <param name="availableNodes">The nodes which can be selected, it is ensured that it contains at least one available node</param>
    /// <param name="agv"></param>
    /// <returns>The best node</returns>
    protected virtual Task<GraphNode> SelectBestNode(IEnumerable<GraphNode> availableNodes, IAgv agv)
    {
        var rand = Random.Shared;
        // must use ToList, otherwise it can not be interpreted
        List<GraphNode> nodes = availableNodes.ToList();
        var node = nodes.ElementAt(rand.Next(nodes.Count));
        return Task.FromResult(node);
    }

    private async Task<GraphNode> GetAgvNode(IAgv agv, GraphNode node)
    {
        var agvGraph = await _agvGraphRegistry.GetAgvGraphAsync(agv.CorrelationId);

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
    protected async Task<bool> IsAgvOnSuchANode(IAgv agv)
    {
        string? lastNodeId = agv.AgvStatus?.LastNodeId;
        if (string.IsNullOrEmpty(lastNodeId))
        {
            return false;
        }
        var currentNode = await _agvGraphRegistry.GetNodeAsync(lastNodeId, agv.CorrelationId);
        bool isSuchANode = NodeFilter.Compile()(currentNode);

        return isSuchANode;
    }
}