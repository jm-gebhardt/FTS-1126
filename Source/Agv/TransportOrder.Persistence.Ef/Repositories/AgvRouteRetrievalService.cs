using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvRouteRetrievalService"/>
public class AgvRouteRetrievalService : IAgvRouteRetrievalService
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IAgvRepository _agvRepository;
    private readonly IAgvRoutePlanCacheProvider _routePlanCache;
    private readonly ILogger<AgvRouteRetrievalService> _logger;

    /// <inheritdoc cref="AgvRouteRetrievalService"/>
    public AgvRouteRetrievalService(
        IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        IAgvRepository agvRepository,
        IAgvRoutePlanCacheProvider routePlanCache,
        ILogger<AgvRouteRetrievalService> logger
    )
    {
        _dbContextFactory = dbContextFactory;
        _agvRepository = agvRepository;
        _routePlanCache = routePlanCache;
        _logger = logger;
    }

    private async Task<TransportOrderDbContext> CreateDbContextAsync()
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        // no need for tracking as we are returning read models only
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return context;
    }

    /// <inheritdoc />
    public async Task<CurrentAgvRouteReadModel?> GetAgvRouteAsync(Guid agvCorrelationId, Guid? lastNodeId = null)
    {
        await using var context = await CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

        Task<ActionGroup?> actionGroupTask = queryable
            .Where(a => a.State == ActionGroupState.Started)
            .Where(a => a.AssignedAgv == agvCorrelationId)
            .OrderByDescending(g => g.StartedDate)
            .FirstOrDefaultAsync();

        Task<Guid?> lastNodeIdTask = lastNodeId == null ? _agvRepository.GetLastNodeIdAsync(agvCorrelationId) : Task.FromResult(lastNodeId);

        await Task.WhenAll(actionGroupTask, lastNodeIdTask);
        var actionGroup = actionGroupTask.Result;
        lastNodeId = lastNodeIdTask.Result;

        var currentAction = actionGroup?.Actions.OrderByDescending(a => a.Sequence).FirstOrDefault(a => a.State == ActionState.Started);

        if (currentAction == null)
        {
            return null;
        }

        GraphNode[] remainingRoute = RoutingHelper.GetRemainingRoute(lastNodeId, currentAction.Nodes);
        Guid[] remainingNodes = currentAction.Edges.Skip(currentAction.Nodes.Length - remainingRoute.Length).ToArray();

        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, remainingRoute, remainingNodes);
        _logger.LogTrace("{AgvCorrelationId} last node Id is: {LastNodeId} and the current route{Route}", agvCorrelationId, lastNodeId, string.Join(" -> ", agvRoute.RemainingNodes.Select(n => n.Name)));

        var plan = _routePlanCache.ConcurrentDict.GetValueOrDefault(agvCorrelationId);
        return agvRoute.AnnotateBase(plan?.BaseNodes.Select(n => n.Id));
    }

    /// <inheritdoc />
    public async Task<ICollection<CurrentAgvRouteReadModel>> GetAgvRouteAsync(ICollection<Guid> agvCorrelationIds)
    {
        await using var context = await CreateDbContextAsync();
        IQueryable<ActionGroup> queryable = context.Set<ActionGroup>()
            .Where(a => a.State == ActionGroupState.Started);

        if (agvCorrelationIds.Count != 0)
        {
            queryable = queryable.Where(a => a.AssignedAgv != null && agvCorrelationIds.Contains((Guid)a.AssignedAgv));
        }

        Task<Dictionary<Guid, ActionGroup?>> actionGroupTask = queryable
            .OrderByDescending(a => a.StartedDate)
            .GroupBy(a => (Guid)a.AssignedAgv!, a => a)
            .Select(g => g.FirstOrDefault())
            .ToDictionaryAsync(a => (Guid)a!.AssignedAgv!, a => a);

        Task<IReadOnlyDictionary<Guid, Guid?>> lastNodeIdTask = _agvRepository.GetLastNodeIdAsync(agvCorrelationIds);

        await Task.WhenAll(actionGroupTask, lastNodeIdTask);
        Dictionary<Guid, ActionGroup?> actionGroupById = actionGroupTask.Result;
        IReadOnlyDictionary<Guid, Guid?> lastNodeIdById = lastNodeIdTask.Result;

        var agvRoutesById = new Dictionary<Guid, CurrentAgvRouteReadModel>();

        foreach (KeyValuePair<Guid, Guid?> lastNodeIdByAgv in lastNodeIdById)
        {
            var agvCorrelationId = lastNodeIdByAgv.Key;
            if (!actionGroupById.TryGetValue(agvCorrelationId, out var actionGroup))
            {
                agvRoutesById[agvCorrelationId] = new CurrentAgvRouteReadModel(agvCorrelationId, [], []);
                continue;
            }

            var currentAction = actionGroup?.Actions
                .OrderByDescending(a => a.Sequence)
                .FirstOrDefault(a => a.State == ActionState.Started);

            if (currentAction == null)
            {
                agvRoutesById[agvCorrelationId] = new CurrentAgvRouteReadModel(agvCorrelationId, [], []);
                continue;
            }

            Guid? lastNodeId = lastNodeIdByAgv.Value;

            GraphNode[] remainingRoute = RoutingHelper.GetRemainingRoute(lastNodeId, currentAction.Nodes);
            Guid[] remainingNodes = currentAction.Edges.Skip(currentAction.Nodes.Length - remainingRoute.Length).ToArray();
            var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, remainingRoute, remainingNodes);
            _logger.LogTrace("{AgvCorrelationId} last node Id is: {LastNodeId} and the current route{Route}", agvCorrelationId, lastNodeId, string.Join(" -> ", agvRoute.RemainingNodes.Select(n => n.Name)));

            var plan = _routePlanCache.ConcurrentDict.GetValueOrDefault(agvCorrelationId);
            agvRoutesById[agvCorrelationId] = agvRoute.AnnotateBase(plan?.BaseNodes.Select(n => n.Id));
        }

        return agvRoutesById.Select(kv => kv.Value).ToList();
    }
}