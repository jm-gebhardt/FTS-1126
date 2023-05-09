using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
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
    public ChargingPointManager(IGraphRegistry graphRegistry, INodeStateRegistry nodeStateRegistry) : base(graphRegistry, nodeStateRegistry)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Charging";

    /// <inheritdoc />
    public Task<AgvGraph.Node> ReserveChargerAsync(IAgv agv)
    {
        return PickAndReserveNodeAsync(agv);
    }

    /// <inheritdoc />
    protected override Expression<Func<Node, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Charge;

    /// <inheritdoc />
    public async Task<AgvGraph.Node?> GetBestChargerAsync(IAgv agv)
    {
        try
        {
            return PickNode(agv);
        }
        catch (NoNodesAvailableException)
        {
            return await Task.FromResult<AgvGraph.Node?>(null);
        }
    }

    /// <inheritdoc />
    protected override Node SelectBestNode(IEnumerable<Node> availableNodes, IAgv agv) => availableNodes.First();

    /// <inheritdoc />
    public Task ReserveChargerAsync(AgvGraph.Node node, Guid agvCorrelationId)
    {
        return ReserveNodeAsync(node.Id, agvCorrelationId);
    }

    /// <inheritdoc />
    public Task<bool> IsOnAChargingNodeAsync(IAgv agv)
    {
        return Task.FromResult(IsAgvOnSuchANode(agv));
    }
}