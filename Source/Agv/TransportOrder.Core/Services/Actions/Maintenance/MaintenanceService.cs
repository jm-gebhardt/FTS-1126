using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Maintenance;

/// <inheritdoc cref="IMaintenanceService"/>
public class MaintenanceService : BaseNodePicker, IMaintenanceService
{
    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="graphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    public MaintenanceService(IGraphRegistry graphRegistry, INodeStateRegistry nodeStateRegistry) : base(graphRegistry, nodeStateRegistry)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Maintenance";

    /// <inheritdoc />
    public bool MaintenanceRequested(IAgv agv)
    {
        return false;
    }

    /// <inheritdoc />
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    /// <exception cref="NodeNotFoundException"></exception>
    public AgvGraph.Node GetMaintenanceNode(IAgv agv)
    {
        return PickNode(agv);
    }

    /// <inheritdoc />
    protected override Expression<Func<Node, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Park;
}