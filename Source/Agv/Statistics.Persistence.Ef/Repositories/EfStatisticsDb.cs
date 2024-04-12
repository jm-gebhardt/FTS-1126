using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IStatisticsDb" />
public class EfStatisticsDb : IStatisticsDb, IAsyncDisposable
{
    private StatisticsDbContext DbContext { get; }

    private readonly bool _efGridRepositoryRegistered;
    private readonly bool _efMapRepositoryRegistered;
    private readonly bool _efAgvGridPositionRepositoryRegistered;
    private readonly bool _efMapAgvPositionRepositoryRegistered;

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

    /// <inheritdoc cref="EfStatisticsDb" />
    public EfStatisticsDb(IServiceProvider provider,
        StatisticsDbContext context,
        IMapRepository mapRepository,
        IGridRepository gridRepository, 
        IGridAgvPositionRepository gridAgvPositionRepository,
        IMapAgvPositionsRepository mapAgvPositionsRepository)
    {
        _efGridRepositoryRegistered = provider.GetService(typeof(IGridRepository)) != null;
        _efMapRepositoryRegistered = provider.GetService(typeof(IMapRepository)) != null;
        _efAgvGridPositionRepositoryRegistered = provider.GetService(typeof(IGridAgvPositionRepository)) != null;
        _efMapAgvPositionRepositoryRegistered = provider.GetService(typeof(IMapAgvPositionsRepository)) != null;
        DbContext = context;
        Maps = mapRepository;
        Grids = gridRepository;
        GridAgvPositions = gridAgvPositionRepository;
        MapAgvPositions = mapAgvPositionsRepository;
    }

    /// <inheritdoc cref="IAsyncDisposable" />
    [UsedImplicitly]
    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }
}