using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Node;

/// <summary>
/// Abstract class to implement when choosing nodes.
/// </summary>
[PublicAPI]
public abstract class BaseNodePicker
{
    /// <summary>
    /// The agv unit of work.
    /// </summary>
    protected readonly IAgvDb AgvDb;

    /// <summary>
    /// Note State repository.
    /// </summary>
    protected readonly INodeStateRepository NodeStateRepository;

    private readonly IRouteService _routeService;
    
    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger<BaseNodePicker> Logger;

    /// <inheritdoc cref="BaseNodePicker"/>>
    protected BaseNodePicker(
        INodeStateRepository nodeStateRepository,
        IRouteService routeService,
        IAgvDb agvDb,
        ILogger<BaseNodePicker> logger)
    {
        NodeStateRepository = nodeStateRepository;
        _routeService = routeService;
        Logger = logger;
        AgvDb = agvDb;
    }

    /// <summary>
    /// For what nodes are you looking for? Used for throwing of exceptions
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The external method to get the actual node which has been selected by the logic 
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="agvData"></param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <returns></returns>
    protected async Task<GraphNode> PickNode(Models.Agv.Agv agv, AgvData? agvData, string? startNodeId = null)
    {
        IEnumerable<GraphNode> availableNodes = await GetAvailableNodes(agv);
        var node = await SelectBestNode(availableNodes, agv, agvData, startNodeId);

        return node;
    }
    
    /// <summary>
    /// Checks if the agv has a node of the given type reserved.
    /// </summary>
    /// <param name="agvCorrelationId">The Agv Correlation Id for which the node should be checked</param>
    /// <param name="nodeType">The node type which should be checked</param>
    /// <returns>>True if the graph of the agv has a node of the given type</returns>
    protected Task<bool> HasAnyNodeOfTypeAsync(Guid agvCorrelationId, AgvGraph.NodeType nodeType)
    {
        return AgvDb.Graphs.HasAnyNodeOfTypeAsync(agvCorrelationId, nodeType);
    }

    private async Task<IEnumerable<GraphNode>> GetAvailableNodes(Models.Agv.Agv agv)
    {
        IEnumerable<GraphNode> nodes = await AgvDb.Graphs.GetNodesAsync(agv.CorrelationId, NodeFilter);
        IEnumerable<GraphNode> availableNodes = NodeStateRepository.FilterFreeUnlockedNodes(nodes);
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
    /// Logic for selection of the best node. This method can be overriden, but it's base implementation does a selected based on the cost of the route
    /// </summary>
    /// <param name="availableNodes">The nodes which can be selected, it is ensured that it contains at least one available node</param>
    /// <param name="agv">The agv</param>
    /// <param name="agvData">The agv data</param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <returns>The best node</returns>
    [PublicAPI]
    protected virtual Task<GraphNode> SelectBestNode(IEnumerable<GraphNode> availableNodes, Models.Agv.Agv agv, AgvData? agvData, string? startNodeId = null)
    {
        Logger.LogDebug("Choosing node with lowest costs");
        return _routeService.GetNodeWithLowestCostsAsync(agv, availableNodes.ToList(), agvData, startNodeId);
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
        var currentNode = await AgvDb.Graphs.GetNodeAsync(lastNodeId);
        bool isSuchANode = NodeFilter.Compile()(currentNode);

        return isSuchANode;
    }
}