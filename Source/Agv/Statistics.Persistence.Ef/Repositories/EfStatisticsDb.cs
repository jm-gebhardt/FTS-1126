using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IStatisticsDb" />
public class EfStatisticsDb : IStatisticsDb, IAsyncDisposable
{
    private StatisticsDbContext DbContext { get; }

    private readonly bool _efGridRepositoryRegistered;
    private readonly bool _efMapRepositoryRegistered;
    private readonly bool _efAgvGridPositionRepositoryRegistered;
    private readonly bool _efMapAgvPositionRepositoryRegistered;
    private readonly bool _efAgvGridErrorRepositoryRegistered;

    /// <inheritdoc />
    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        // Ensure transaction safety even if non-EfCore versions of repositories are registered.
        if (!_efGridRepositoryRegistered)
        {
            await Grids.SaveAsync();
        }

        if (!_efMapRepositoryRegistered)
        {
            await Maps.SaveAsync();
        }

        if (!_efAgvGridPositionRepositoryRegistered)
        {
            await GridAgvPositions.SaveAsync();
        }
        
        if(!_efMapAgvPositionRepositoryRegistered)
        {
            await MapAgvPositions.SaveAsync();
        }

        if (!_efAgvGridErrorRepositoryRegistered)
        {
            await GridAgvErrors.SaveAsync();
        }

        return await DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IMapRepository Maps { get; }

    /// <inheritdoc />
    public IGridRepository Grids { get; }

    /// <inheritdoc />
    public IGridAgvPositionRepository GridAgvPositions { get; }
    
    /// <inheritdoc />
    public IMapAgvPositionsRepository MapAgvPositions { get; }

    /// <inheritdoc />
    public IAgvErrorRepository GridAgvErrors { get; }

    /// <inheritdoc cref="EfStatisticsDb" />
    public EfStatisticsDb(IServiceProvider provider,
        StatisticsDbContext context,
        IMapRepository mapRepository,
        IGridRepository gridRepository, 
        IGridAgvPositionRepository gridAgvPositionRepository,
        IMapAgvPositionsRepository mapAgvPositionsRepository,
        IAgvErrorRepository gridAgvErrorRepository
        )
    {
        _efGridRepositoryRegistered = provider.GetService(typeof(EfGridRepository)) != null;
        _efMapRepositoryRegistered = provider.GetService(typeof(EfMapRepository)) != null;
        _efAgvGridPositionRepositoryRegistered = provider.GetService(typeof(EfGridAgvPositionRepository)) != null;
        _efMapAgvPositionRepositoryRegistered = provider.GetService(typeof(EfMapAgvPositionsRepository)) != null;
        _efAgvGridErrorRepositoryRegistered = provider.GetService(typeof(EfAgvErrorRepository)) != null;
        DbContext = context;
        Maps = mapRepository;
        Grids = gridRepository;
        GridAgvPositions = gridAgvPositionRepository;
        MapAgvPositions = mapAgvPositionsRepository;
        GridAgvErrors = gridAgvErrorRepository;
    }

    /// <inheritdoc cref="IAsyncDisposable" />
    [UsedImplicitly]
    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }
}