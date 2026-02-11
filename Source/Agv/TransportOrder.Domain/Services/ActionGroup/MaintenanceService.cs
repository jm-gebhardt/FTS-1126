using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Node;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IMaintenanceService"/>
public class MaintenanceService : BaseNodePicker, IMaintenanceService
{
    /// <inheritdoc cref="MaintenanceService"/>>
    public MaintenanceService(IAgvDb agvDb, INodeRepository nodeRepository, IRouteService routeService, ILogger<MaintenanceService> logger)
        : base(nodeRepository, routeService, agvDb, logger)
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
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.IsParker();
}