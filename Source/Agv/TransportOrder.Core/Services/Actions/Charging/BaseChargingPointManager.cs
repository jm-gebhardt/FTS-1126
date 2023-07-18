using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;

/// <inheritdoc cref="IChargingPointManager"/>
public class ChargingPointManager : BaseNodeReserve, IChargingPointManager
{
    /// <summary>
    /// Constructor for base charging point manager
    /// </summary>
    /// <param name="graphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    public ChargingPointManager(IAgvGraphRegistry graphRegistry, INodeStateRegistry nodeStateRegistry) : base(graphRegistry, nodeStateRegistry)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Charging";

    /// <inheritdoc />
    public Task<GraphNode> ReserveChargerAsync(IAgv agv)
    {
        return PickAndReserveNodeAsync(agv);
    }

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Charge;

    /// <inheritdoc />
    public async Task<GraphNode?> GetBestChargerAsync(IAgv agv)
    {
        try
        {
            return await PickNode(agv);
        }
        catch (NoNodesAvailableException)
        {
            return await Task.FromResult<GraphNode?>(null);
        }
    }

    /// <inheritdoc />
    protected override Task<GraphNode> SelectBestNode(IEnumerable<GraphNode> availableNodes, IAgv agv) => Task.FromResult(availableNodes.First());

    /// <inheritdoc />
    public Task ReserveChargerAsync(GraphNode node, Guid agvCorrelationId)
    {
        return ReserveNodeAsync(node.Id, agvCorrelationId);
    }

    /// <inheritdoc />
    public async Task<bool> IsOnAChargingNodeAsync(IAgv agv)
    {
        return await IsAgvOnSuchANode(agv);
    }
}