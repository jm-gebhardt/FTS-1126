using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Node;

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

    private readonly IRouteService _routeService;
    private readonly ILogger<BaseNodePicker> _logger;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="agvGraphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    protected BaseNodePicker(IAgvGraphRegistry agvGraphRegistry, INodeStateRegistry nodeStateRegistry, IRouteService routeService, ILogger<BaseNodePicker> logger)
    {
        _agvGraphRegistry = agvGraphRegistry;
        NodeStateRegistry = nodeStateRegistry;
        _routeService = routeService;
        _logger = logger;
    }

    /// <summary>
    /// For what nodes are you looking for? Used for throwing of exceptions
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The external method to get the actual node which has been selected by the logic 
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected async Task<GraphNode> PickNode(Models.Agv.Agv agv)
    {
        IEnumerable<GraphNode> availableNodes = await GetAvailableNodes(agv);
        var node = await SelectBestNode(availableNodes, agv);

        return node;
    }

    private async Task<IEnumerable<GraphNode>> GetAvailableNodes(Models.Agv.Agv agv)
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
    /// Logic for selection of best node. This method can be overriden, but it's base implementation does a lowest cost route selection
    /// </summary>
    /// <param name="availableNodes">The nodes which can be selected, it is ensured that it contains at least one available node</param>
    /// <param name="agv"></param>
    /// <returns>The best node</returns>
    [PublicAPI]    
    protected virtual Task<GraphNode> SelectBestNode(IEnumerable<GraphNode> availableNodes, Models.Agv.Agv agv)
    {
        _logger.LogDebug("Choosing node with lowest costs");
        return _routeService.GetNodeWithLowestCostsAsync(agv, availableNodes.ToList());
    }

    /// <summary>
    /// Returns random selection of node from available nodes
    /// </summary>
    /// <param name="availableNodes"></param>
    /// <param name="agv"></param>
    /// <returns></returns>
    [PublicAPI]
    protected virtual Task<GraphNode> SelectRandomAvailableNode(IEnumerable<GraphNode> availableNodes, Models.Agv.Agv agv)
    {
        var rand = Random.Shared;
        // must use ToList, otherwise it can not be interpreted
        List<GraphNode> nodes = availableNodes.ToList();
        var node = nodes.ElementAt(rand.Next(nodes.Count));
        return Task.FromResult(node);
    }

    /// <summary>
    /// Checks if the agv is on such a node defined by the <see cref="NodeFilter"/>
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>True if the lastNodeId of the Agv is such a node, False if the lastNodeId of the agv is null</returns>
    protected async Task<bool> IsAgvOnSuchANode(Models.Agv.Agv agv)
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