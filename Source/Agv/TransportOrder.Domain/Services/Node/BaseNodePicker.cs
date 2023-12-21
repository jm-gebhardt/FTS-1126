using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Node;

/// <summary>
/// Abstract class to implement when choosing nodes.
/// </summary>
public abstract class BaseNodePicker
{
    /// <summary>
    /// The graph repository.
    /// </summary>
    private readonly IGraphRepository _graphRepository;

    /// <summary>
    /// Note State repository.
    /// </summary>
    protected readonly INodeStateRepository NodeStateRepository;

    /// <summary>
    /// <see cref="AgvData"/> repository.
    /// </summary>
    protected readonly IAgvDataRepository AgvDataRepository;

    private readonly IRouteService _routeService;
    private readonly ILogger<BaseNodePicker> _logger;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="graphRepository">The graph repository.</param>
    /// <param name="nodeStateRepository"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    /// <param name="agvDataRepository">The <see cref="AgvData"/> repository.</param>
    protected BaseNodePicker(IGraphRepository graphRepository,
        INodeStateRepository nodeStateRepository,
        IRouteService routeService,
        IAgvDataRepository agvDataRepository,
        ILogger<BaseNodePicker> logger)
    {
        _graphRepository = graphRepository;
        NodeStateRepository = nodeStateRepository;
        _routeService = routeService;
        _logger = logger;
        AgvDataRepository = agvDataRepository;
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
    /// <returns></returns>
    protected async Task<GraphNode> PickNode(Models.Agv.Agv agv, AgvData? agvData)
    {
        IEnumerable<GraphNode> availableNodes = await GetAvailableNodes(agv);
        var node = await SelectBestNode(availableNodes, agv, agvData);

        return node;
    }

    private async Task<IEnumerable<GraphNode>> GetAvailableNodes(Models.Agv.Agv agv)
    {
        IEnumerable<GraphNode> nodes = await _graphRepository.GetNodesAsync(agv.CorrelationId, NodeFilter);
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
    /// Logic for selection of best node. This method can be overriden, but it's base implementation does a lowest cost route selection
    /// </summary>
    /// <param name="availableNodes">The nodes which can be selected, it is ensured that it contains at least one available node</param>
    /// <param name="agv">The agv</param>
    /// <param name="agvData">The agv data</param>
    /// <returns>The best node</returns>
    [PublicAPI]
    protected virtual Task<GraphNode> SelectBestNode(IEnumerable<GraphNode> availableNodes, Models.Agv.Agv agv, AgvData? agvData)
    {
        _logger.LogDebug("Choosing node with lowest costs");
        return _routeService.GetNodeWithLowestCostsAsync(agv, availableNodes.ToList(), agvData);
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
        var currentNode = await _graphRepository.GetNodeAsync(lastNodeId);
        bool isSuchANode = NodeFilter.Compile()(currentNode);

        return isSuchANode;
    }
}