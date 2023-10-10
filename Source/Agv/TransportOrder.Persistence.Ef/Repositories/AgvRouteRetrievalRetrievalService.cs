using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvRouteRetrievalService"/>
public class AgvRouteRetrievalRetrievalService : IAgvRouteRetrievalService
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IAgvRepository _agvRepository;
    private readonly ILogger<AgvRouteRetrievalRetrievalService> _logger;

    /// <inheritdoc cref="AgvRouteRetrievalRetrievalService"/>
    public AgvRouteRetrievalRetrievalService(
        IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        IAgvRepository agvRepository,
        ILogger<AgvRouteRetrievalRetrievalService> logger
    )
    {
        _dbContextFactory = dbContextFactory;
        _agvRepository = agvRepository;
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
    public async Task<AgvRouteReadModel?> GetAgvRouteAsync(Guid agvCorrelationId, string? lastNodeId = null)
    {
        await using var context = await CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

        Task<ActionGroup?> actionGroupTask = queryable
            .Where(a => a.State == ActionGroupState.Started)
            .Where(a => a.AssignedAgv == agvCorrelationId)
            .OrderByDescending(g => g.StartedDate)
            .FirstOrDefaultAsync();

        // if lastNodeId parameter is not we want to get the current value from the registry
        Task<string?> lastNodeIdTask = string.IsNullOrEmpty(lastNodeId) ? _agvRepository.GetLastNodeIdAsync(agvCorrelationId) : Task.FromResult<string?>(lastNodeId);

        await Task.WhenAll(actionGroupTask, lastNodeIdTask);
        var actionGroup = actionGroupTask.Result;
        lastNodeId = lastNodeIdTask.Result;

        var currentAction = actionGroup?.Actions.OrderByDescending(a => a.Sequence).FirstOrDefault(a => a.State == ActionState.Started);

        if (currentAction == null)
        {
            return null;
        }

        string[] remainingRoute = RoutingHelper.GetRemainingRoute(lastNodeId, currentAction.Nodes);

        var agvRoute = new AgvRouteReadModel(remainingRoute);
        _logger.LogTrace("{AgvCorrelationId} last node Id is: {LastNodeId} and the current route{Route}", agvCorrelationId, lastNodeId, string.Join(" -> ", agvRoute.RemainingRoute));

        return agvRoute;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, AgvRouteReadModel?>> GetAgvRouteAsync(ICollection<Guid> agvCorrelationIds)
    {
        await using var context = await CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

        Task<Dictionary<Guid, ActionGroup?>> actionGroupTask = queryable
            .Where(a => a.State == ActionGroupState.Started)
            .Where(a => a.AssignedAgv != null && agvCorrelationIds.Contains((Guid) a.AssignedAgv))
            .OrderByDescending(a => a.StartedDate)
            .GroupBy(a => (Guid) a.AssignedAgv!, a => a)
            .Select(g => g.FirstOrDefault())
            .ToDictionaryAsync(a => (Guid) a!.AssignedAgv!, a => a);

        Task<IReadOnlyDictionary<Guid, string?>> lastNodeIdTask = _agvRepository.GetLastNodeIdAsync(agvCorrelationIds);

        await Task.WhenAll(actionGroupTask, lastNodeIdTask);
        Dictionary<Guid, ActionGroup?> actionGroupById = actionGroupTask.Result;
        IReadOnlyDictionary<Guid, string?> lastNodeIdById = lastNodeIdTask.Result;

        var agvRoutesById = new Dictionary<Guid, AgvRouteReadModel?>();

        foreach (var agvCorrelationId in agvCorrelationIds)
        {
            if (!actionGroupById.TryGetValue(agvCorrelationId, out var actionGroup))
            {
                agvRoutesById[agvCorrelationId] = null;
                continue;
            }

            var currentAction = actionGroup?.Actions
                .OrderByDescending(a => a.Sequence)
                .FirstOrDefault(a => a.State == ActionState.Started);

            if (currentAction == null)
            {
                agvRoutesById[agvCorrelationId] = null;
                continue;
            }

            lastNodeIdById.TryGetValue(agvCorrelationId, out string? lastNodeId);

            string[] remainingRoute = RoutingHelper.GetRemainingRoute(lastNodeId, currentAction.Nodes);

            var agvRoute = new AgvRouteReadModel(remainingRoute);
            _logger.LogTrace("{AgvCorrelationId} last node Id is: {LastNodeId} and the current route{Route}", agvCorrelationId, lastNodeId, string.Join(" -> ", agvRoute.RemainingRoute));

            agvRoutesById[agvCorrelationId] = agvRoute;
        }

        return agvRoutesById;
    }
}