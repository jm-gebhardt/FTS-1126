using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IMaintenanceService"/>
public class MaintenanceService : BaseNodePicker, IMaintenanceService
{
    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="agvGraphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    public MaintenanceService(IAgvGraphRegistry agvGraphRegistry, INodeStateRegistry nodeStateRegistry, IRouteService routeService, ILogger<MaintenanceService> logger)
        : base(agvGraphRegistry, nodeStateRegistry, routeService, logger)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Maintenance";

    /// <inheritdoc />
    public bool MaintenanceRequested(Models.Agv.Agv agv)
    {
        return false;
    }

    /// <inheritdoc />
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    /// <exception cref="NodeNotFoundException"></exception>
    public async Task<GraphNode> GetMaintenanceNode(Models.Agv.Agv agv)
    {
        return await PickNode(agv);
    }

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Park;
}