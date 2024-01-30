using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IMaintenanceService"/>
public class MaintenanceService : BaseNodePicker, IMaintenanceService
{
    /// <inheritdoc cref="MaintenanceService"/>>
    public MaintenanceService(IAgvDb agvDb, INodeStateRepository nodeStateRepository, IRouteService routeService, ILogger<MaintenanceService> logger)
        : base(nodeStateRepository, routeService, agvDb, logger)
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
        var agvData = await AgvDb.Data.GetAgvDataAsync(agv.CorrelationId);
        return await PickNode(agv, agvData);
    }

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Park;
}