using AutoMapper;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class AgvRepository : IAgvRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel> _agvSettingsProvider;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AgvRepository"/> instance.
    /// </summary>
    public AgvRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel> agvSettingsProvider, IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _agvSettingsProvider = agvSettingsProvider;
        _mapper = mapper;
    }

    /// <summary>
    /// Initializes a new <see cref="AgvRepository"/> instance for db only access.
    /// </summary>
    public AgvRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _agvSettingsProvider = null!;
        _mapper = null!;
    }

    private IQueryable<Agv> Agvs =>
        _dbContextFactory.CreateDbContext()
            .Set<Agv>()
            .AsNoTracking(); // No need to track changes

    /// <inheritdoc />
    public int Count => Agvs.Count();

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetAllAgvsAsync(CancellationToken cancellationToken = default)
    {
        return await Agvs.ToListAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetAllAgvsWithGraphAsync()
    {
        return await Agvs.Where(a => a.GraphExists).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetTypeIdsWithGraphAsync()
    {
        var ids = await Agvs
            .Where(a => a.GraphExists && a.AgvTypeId != null)
            .Select(a => a.AgvTypeId!.Value)
            .Distinct()
            .ToListAsync();
        return ids.ToHashSet();
    }

    /// <inheritdoc />
    public async Task<bool> HasOnlineAgvs()
    {
        return await Agvs.AnyAsync(a => a.GraphExists && a.IsOnline);
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetIdleAgvsAsync()
    {
        return await Agvs.Where(x => x.CurrentAgvState == AgvState.Idle).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Agv> GetAgvAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var agv = await TryGetAgvAsync(correlationId, cancellationToken);
        if (agv == null)
        {
            throw new AgvNotFoundException(correlationId);
        }
        return agv;
    }

    /// <inheritdoc />
    public async Task<Agv?> GetAnyOnlineAgv()
    {
        return await Agvs.OrderBy(a => a.TimeOfStateTransition).FirstOrDefaultAsync(a => a.GraphExists && a.IsOnline);
    }

    /// <inheritdoc />
    public async Task<Agv?> TryGetAgvAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var agv = await context.Set<Agv>().Where(a => a.CorrelationId == correlationId).SingleOrDefaultAsync(cancellationToken);
        return agv;
    }

    /// <inheritdoc />
    public async Task<bool> CheckIfAgvExistsAsync(Guid correlationId)
    {
        return await Agvs.AnyAsync(a => a.CorrelationId == correlationId);
    }

    private async Task<ICollection<Guid>> CheckIfAgvExistAsync(IEnumerable<Guid> correlationIds)
    {
        return await Agvs.Where(a => correlationIds.Contains(a.CorrelationId)).Select(a => a.CorrelationId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task ThrowIfAgvNotExistsAsync(Guid correlationId)
    {
        if (!await CheckIfAgvExistsAsync(correlationId))
        {
            throw new AgvNotFoundException(correlationId);
        }
    }

    /// <inheritdoc />
    public async Task ThrowIfAgvsNotExistAsync(ICollection<Guid> correlationIds)
    {
        var existingAgvs = await CheckIfAgvExistAsync(correlationIds);
        var notExistingAgvs = correlationIds.Except(existingAgvs).ToList();
        if (notExistingAgvs.Count != 0)
        {
            throw new AgvsNotFoundException(notExistingAgvs);
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> SelectExistingAgvsAsync(List<Guid> agvCorrelationIds)
    {
        return await Agvs.Where(a => agvCorrelationIds.Count == 0 || agvCorrelationIds.Contains(a.CorrelationId)).OrderBy(a => a.SerialNumber).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> SelectAgvsWithAboveCriticalBatteryLevel(ICollection<Guid> agvCorrelationIds)
    {
        if (agvCorrelationIds.Count == 0)
        {
            return [];
        }
        var agvs = await Agvs
            .Where(a => agvCorrelationIds.Contains(a.CorrelationId))
            .Where(a => !a.IsLocked &&
                        !a.IsQueueing &&
                        a.IsOnline &&
                        a.Mode.Type == ModeType.Normal).ToListAsync();
        return agvs.Where(a => a.AgvStatus!.BatteryCharge >= _agvSettingsProvider.ConcurrentDict[a.AgvSettingsGroup].BatteryLevelCritical).ToList();
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> SelectInfinitelyChargingAgvsWithAboveMinChargeUpBatteryLevel(ICollection<Guid> agvCorrelationIds)
    {
        if (agvCorrelationIds.Count == 0)
        {
            return [];
        }
        var agvs = await Agvs
            .Where(a => agvCorrelationIds.Contains(a.CorrelationId))
            .Where(a => !a.IsLocked &&
                        !a.IsQueueing &&
                        a.IsOnline &&
                        a.Mode.Type == ModeType.Normal).ToListAsync();
        return agvs.Where(a =>
            a.AgvStatus!.BatteryCharge >= _agvSettingsProvider.ConcurrentDict[a.AgvSettingsGroup].MinChargeUp &&
            _agvSettingsProvider.ConcurrentDict[a.AgvSettingsGroup].InfiniteCharging).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAgvAsync(Agv agv, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var dbSet = dbContext.Set<Agv>();
        bool addStatusNotRequired= agv.AgvStatus == null || await dbSet.AnyAsync(a => a.CorrelationId == agv.CorrelationId && a.AgvStatus != null, cancellationToken);
        dbSet.Update(agv);
        if (!addStatusNotRequired)
        {
            var statusEntry = dbContext.Entry(agv).Reference(a => a.AgvStatus).TargetEntry;
            statusEntry?.State = EntityState.Added;
        }
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    /// <inheritdoc />
    public async Task<bool> AddAgvAsync(Agv agv)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var dbSet = dbContext.Set<Agv>();
        dbSet.Add(agv);
        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetReadyAgvsWithAtLeastLowBatteryLevel()
    {
        var agvs = await Agvs.Where(Agv.IsReadyStatic)
            .Where(a => a.GraphExists &&
                        !a.IsLocked &&
                        !a.IsQueueing &&
                        a.Mode.Type == ModeType.Normal &&
                        a.CurrentAgvState == AgvState.Idle &&
                        !a.AgvStatus!.Busy).ToListAsync();

        return agvs.Where(a => a.AgvStatus!.BatteryCharge >= _agvSettingsProvider.ConcurrentDict[a.AgvSettingsGroup].BatteryLevelLow).ToList();
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetChargingAgvsWithMinBatteryLevelAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var actionGroups = context.Set<ActionGroup>().AsNoTracking();

        var agvContext = context.Set<Agv>().AsNoTracking();
        var agvs = await agvContext.Where(agv => agv.GraphExists &&
                                                 actionGroups.Any(g => g.AssignedAgv == agv.CorrelationId && g.State == ActionGroupState.Started &&
                                                                       g.Actions.Any(a => a.ActionType == ActionType.Charge && a.State == ActionState.Started)) &&
                                                 agv.CurrentAgvState == AgvState.Busy && agv.AgvStatus!.Charging && !agv.IsLocked).ToListAsync();
        return agvs.Where(a => a.AgvStatus!.BatteryCharge >= _agvSettingsProvider.ConcurrentDict[a.AgvSettingsGroup].MinChargeUp).ToList();
    }

    private async Task<List<Agv>> GetAgvsByChargeCondition(Func<Agv, bool> chargePredicate)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Occupied node locks 
        var nodeLocks = await context.Set<NodeLock>()
            .Where(nl => nl.NodeState == NodeLockState.Occupied)
            .Select(nl => new { nl.NodeId, nl.AgvCorrelationId })
            .ToListAsync();
        var nodeIds = nodeLocks.Select(nl => nl.NodeId).Distinct().ToList();

        // Nodes corresponding to locks to get which are chargers
        var efNodes = await context.Set<GraphNode>()
            .Where(en => nodeIds.Contains(en.Id))
            .ToListAsync();
        var lockedNodes = _mapper.Map<List<GraphNode>>(efNodes);

        var chargerNodeIds = lockedNodes
            .Where(n => n.IsCharger())
            .Select(n => n.Id)
            .ToHashSet();

        // Charging agvs
        var chargingAgvIds = nodeLocks
            .Where(nl => chargerNodeIds.Contains(nl.NodeId))
            .Select(nl => nl.AgvCorrelationId)
            .Distinct()
            .ToList();

        var agvs = await context.Set<Agv>()
            .AsNoTracking()
            .Where(agv => chargingAgvIds.Contains(agv.CorrelationId))
            .ToListAsync();

        return agvs
            .Where(agv => agv.AgvStatus != null && chargePredicate(agv))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel(double chargeLevel, Guid agvCorrelationId)
    {
        var agvs = await GetAgvsByChargeCondition(agv => agv.AgvStatus!.BatteryCharge >= chargeLevel);
        return await FilterByAgvReachabilityAsync(agvs, agvCorrelationId);
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel(Guid? agvCorrelationId = null)
    {
        var agvs = await GetAgvsByChargeCondition(agv => agv.AgvStatus!.BatteryCharge >= _agvSettingsProvider.ConcurrentDict[agv.AgvSettingsGroup].MinChargeUp);
        if (agvCorrelationId == null)
        {
            return agvs;
        }
        return await FilterByAgvReachabilityAsync(agvs, agvCorrelationId.Value);
    }

    private async Task<ICollection<Agv>> FilterByAgvReachabilityAsync(ICollection<Agv> agvs, Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var agvGraph = await context.Set<AgvGraph>().Include(g => g.Nodes).SingleOrDefaultAsync(g => g.AgvCorrelationId == agvCorrelationId);
        if (agvGraph == null)
        {
            return [];
        }
        return agvs.Where(a => agvGraph.Nodes.Any(n => n.Id == a.AgvStatus?.LastNodeId)).ToList();
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetNotChargingAgvsWithCriticalBatteryLevelThatCanReachCharger(Guid? charger)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Occupied or reserved node locks 
        var nodeLocks = await context.Set<NodeLock>()
            .Where(nl => nl.NodeState == NodeLockState.Occupied || nl.NodeState == NodeLockState.Reserved)
            .Select(nl => new { nl.NodeId, nl.AgvCorrelationId })
            .ToListAsync();
        var nodeIds = nodeLocks.Select(nl => nl.NodeId).Distinct().ToList();

        // Nodes corresponding to locks to get which are chargers
        var efNodes = await context.Set<GraphNode>()
            .Where(en => nodeIds.Contains(en.Id))
            .ToListAsync();
        var lockedNodes = _mapper.Map<List<GraphNode>>(efNodes);

        var chargerNodeIds = lockedNodes
            .Where(n => n.IsCharger())
            .Select(n => n.Id)
            .ToHashSet();

        // Charging agvs
        var chargingAgvIds = nodeLocks
            .Where(nl => chargerNodeIds.Contains(nl.NodeId))
            .Select(nl => nl.AgvCorrelationId)
            .Distinct()
            .ToList();

        var agvs = await context.Set<Agv>()
            .AsNoTracking()
            .Where(agv => !chargingAgvIds.Contains(agv.CorrelationId))
            .ToListAsync();
        var agvsThatCanReachCharger = agvs.Select(a => a.CorrelationId).ToHashSet();
        if (charger != null)
        {
            agvsThatCanReachCharger = await context.Set<AgvGraph>().Include(a => a.Nodes)
                .Where(a => a.Nodes.Any(n => n.Id == charger))
                .Select(a => a.AgvCorrelationId).ToHashSetAsync();
        }
        return agvs
            .Where(agv => agv.AgvStatus != null &&
                          agv.AgvStatus.BatteryCharge <= _agvSettingsProvider.ConcurrentDict[agv.AgvSettingsGroup].BatteryLevelCritical
                          && agvsThatCanReachCharger.Contains(agv.CorrelationId))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Guid?> GetLastNodeIdAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var lastNodeId = await Agvs
            .Where(s => s.CorrelationId == agvCorrelationId && s.AgvStatus != null)
            .Select(s => s.AgvStatus!.LastNodeId)
            .SingleOrDefaultAsync();

        return lastNodeId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, Guid?>> GetLastNodeIdAsync(ICollection<Guid> agvCorrelationIds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var queryable = Agvs;
        if (agvCorrelationIds.Count != 0)
        {
            queryable = queryable.Where(s => agvCorrelationIds.Contains(s.CorrelationId));
        }

        var lastNodeIdsById = await queryable
            .Where(s => s.AgvStatus != null)
            .Select(s => new { s.CorrelationId, s.AgvStatus!.LastNodeId })
            .ToDictionaryAsync(x => x.CorrelationId, x => x.LastNodeId);

        // for potential Agvs with no status, we want to have a null value
        foreach (var agvCorrelationId in agvCorrelationIds)
        {
            lastNodeIdsById.TryAdd(agvCorrelationId, null);
        }

        return lastNodeIdsById;
    }
}