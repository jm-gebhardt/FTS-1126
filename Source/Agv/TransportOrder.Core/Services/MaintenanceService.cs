using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;

/// <inheritdoc/>
public class MaintenanceService : IMaintenanceService
{
    private readonly IGraphRegistry _graphRegistry;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="graphRegistry">The graph registry</param>
    public MaintenanceService(IGraphRegistry graphRegistry)
    {
        _graphRegistry = graphRegistry;
    }

    /// <inheritdoc />
    public bool MaintenanceRequested(IAgv agv)
    {
        return false;
    }

    /// <inheritdoc />
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    /// <exception cref="WaypointNotFoundException"></exception>
    public AgvGraph.Node GetMaintenanceNode(IAgv agv)
    {
        var agvGraph = agv.AgvGraph;
        if (agvGraph == null)
        {
            throw new AgvGraphNotDefinedException(agv);
        }

        List<Node>? freeMaintenanceNodes = _graphRegistry.GetNodes(n => n.State == AgvGraph.NodeState.Free && n.Type == AgvGraph.NodeType.Park).ToList();
        if (freeMaintenanceNodes == null || !freeMaintenanceNodes.Any())
        {
            throw new WaypointNotFoundException(agv, "Free park point");
        }
        var rand = Random.Shared;
        var node = freeMaintenanceNodes.ElementAt(rand.Next(freeMaintenanceNodes.Count));

        var agvNode = agvGraph.Nodes.FirstOrDefault(n => n.Id == node.Id);
        if (agvNode == null)
        {
            throw new WaypointNotFoundException(agv, $"NodeId: {node.Id} not found in AgvGraph");
        }
        return agvNode;
    }
}