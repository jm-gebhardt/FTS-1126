using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

/// <inheritdoc />
public class BaseChargingPointManager : IChargingPointManager
{
    private readonly IGraphRegistry _graphRegistry;
    private readonly SemaphoreSlim _reserveSemaphoreSlim = new(1, 1);

    /// <summary>
    /// Constructor for base charging point manager
    /// </summary>
    /// <param name="graphRegistry">The graph registry</param>
    public BaseChargingPointManager(IGraphRegistry graphRegistry)
    {
        _graphRegistry = graphRegistry;
    }

    /// <inheritdoc />
    public AgvGraph.Node ReserveBestCharger(AgvGraph agvGraph)
    {
        _reserveSemaphoreSlim.Wait();
        try
        {
            var bestChargerNode = GetBestCharger(agvGraph);
            if (bestChargerNode == null)
            {
                throw new Exception("No charger found");
            }

            return ReserveCharger(bestChargerNode) ? bestChargerNode : throw new Exception("Could not reserve charger");
        }
        finally
        {
            _reserveSemaphoreSlim.Release();
        }
    }

    /// <inheritdoc />
    public AgvGraph.Node? GetBestCharger(AgvGraph agvGraph)
    {
        return agvGraph.Nodes.FirstOrDefault(node => node is { Type: AgvGraph.NodeType.Charge, State: AgvGraph.NodeState.Free });
    }

    /// <inheritdoc />
    public bool ReserveCharger(AgvGraph.Node node)
    {
        return node.Type == AgvGraph.NodeType.Charge && _graphRegistry.ReserveNode(node.Id);
    }
}