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
    public async Task<ICollection<Agv>> GetAllAgvsAsync()
    {
        return await Agvs.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetAllAgvsWithGraphAsync()
    {
        return await Agvs.Where(a => a.GraphExists).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetTypeIdsWithGraphAsync()
    {
        List<Guid> ids = await Agvs
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
    public async Task<Agv> GetAgvAsync(Guid correlationId)
    {
        var agv = await TryGetAgvAsync(correlationId);
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
    public async Task<Agv?> TryGetAgvAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var agv = await context.Set<Agv>().Where(a => a.CorrelationId == correlationId).SingleOrDefaultAsync();
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
        ICollection<Guid> existingAgvs = await CheckIfAgvExistAsync(correlationIds);
        List<Guid> notExistingAgvs = correlationIds.Except(existingAgvs).ToList();
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
        List<Agv> agvs = await Agvs
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
        List<Agv> agvs = await Agvs
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
    public async Task<bool> UpdateAgvAsync(Agv agv)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        DbSet<Agv> dbSet = dbContext.Set<Agv>();
        dbSet.Update(agv);
        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <inheritdoc />
    public async Task<bool> AddAgvAsync(Agv agv)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        DbSet<Agv> dbSet = dbContext.Set<Agv>();
        dbSet.Add(agv);
        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetReadyAgvsWithAtLeastLowBatteryLevel()
    {
        List<Agv> agvs = await Agvs.Where(Agv.IsReadyStatic)
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
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>().AsNoTracking();

        IQueryable<Agv> agvContext = context.Set<Agv>().AsNoTracking();
        List<Agv> agvs = await agvContext.Where(agv => agv.GraphExists &&
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
        List<Guid> nodeIds = nodeLocks.Select(nl => nl.NodeId).Distinct().ToList();

        // Nodes corresponding to locks to get which are chargers
        List<GraphNode> efNodes = await context.Set<GraphNode>()
            .Where(en => nodeIds.Contains(en.Id))
            .ToListAsync();
        var lockedNodes = _mapper.Map<List<GraphNode>>(efNodes);

        HashSet<Guid> chargerNodeIds = lockedNodes
            .Where(n => n.IsCharger())
            .Select(n => n.Id)
            .ToHashSet();

        // Charging agvs
        List<Guid?> chargingAgvIds = nodeLocks
            .Where(nl => chargerNodeIds.Contains(nl.NodeId))
            .Select(nl => nl.AgvCorrelationId)
            .Distinct()
            .ToList();

        List<Agv> agvs = await context.Set<Agv>()
            .AsNoTracking()
            .Where(agv => chargingAgvIds.Contains(agv.CorrelationId))
            .ToListAsync();

        return agvs
            .Where(agv => agv.AgvStatus != null && chargePredicate(agv))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetAgvsOnChargerWithAtLeastChargeUpLevel(double chargeLevel)
    {
        return await GetAgvsByChargeCondition(agv => agv.AgvStatus!.BatteryCharge >= chargeLevel);
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetAgvsOnChargerWithAtLeastMinChargeUpLevel()
    {
        return await GetAgvsByChargeCondition(agv => agv.AgvStatus!.BatteryCharge >= _agvSettingsProvider.ConcurrentDict[agv.AgvSettingsGroup].MinChargeUp);
    }

    /// <inheritdoc />
    public async Task<ICollection<Agv>> GetNotChargingAgvsWithCriticalBatteryLevel()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Occupied or reserved node locks 
        var nodeLocks = await context.Set<NodeLock>()
            .Where(nl => nl.NodeState == NodeLockState.Occupied || nl.NodeState == NodeLockState.Reserved)
            .Select(nl => new { nl.NodeId, nl.AgvCorrelationId })
            .ToListAsync();
        List<Guid> nodeIds = nodeLocks.Select(nl => nl.NodeId).Distinct().ToList();

        // Nodes corresponding to locks to get which are chargers
        List<GraphNode> efNodes = await context.Set<GraphNode>()
            .Where(en => nodeIds.Contains(en.Id))
            .ToListAsync();
        var lockedNodes = _mapper.Map<List<GraphNode>>(efNodes);

        HashSet<Guid> chargerNodeIds = lockedNodes
            .Where(n => n.IsCharger())
            .Select(n => n.Id)
            .ToHashSet();

        // Charging agvs
        List<Guid?> chargingAgvIds = nodeLocks
            .Where(nl => chargerNodeIds.Contains(nl.NodeId))
            .Select(nl => nl.AgvCorrelationId)
            .Distinct()
            .ToList();

        List<Agv> agvs = await context.Set<Agv>()
            .AsNoTracking()
            .Where(agv => !chargingAgvIds.Contains(agv.CorrelationId))
            .ToListAsync();

        return agvs
            .Where(agv => agv.AgvStatus != null &&
                          agv.AgvStatus.BatteryCharge <= _agvSettingsProvider.ConcurrentDict[agv.AgvSettingsGroup].BatteryLevelCritical)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Guid?> GetLastNodeIdAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        Guid? lastNodeId = await Agvs
            .Where(s => s.CorrelationId == agvCorrelationId && s.AgvStatus != null)
            .Select(s => s.AgvStatus!.LastNodeId)
            .SingleOrDefaultAsync();

        return lastNodeId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, Guid?>> GetLastNodeIdAsync(ICollection<Guid> agvCorrelationIds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        IQueryable<Agv> queryable = Agvs;
        if (agvCorrelationIds.Count != 0)
        {
            queryable = queryable.Where(s => agvCorrelationIds.Contains(s.CorrelationId));
        }

        Dictionary<Guid, Guid?> lastNodeIdsById = await queryable
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