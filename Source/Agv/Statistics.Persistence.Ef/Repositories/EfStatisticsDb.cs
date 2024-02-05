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
            await GridPositions.SaveAsync();
        }

        return await DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IMapRepository Maps { get; }

    /// <inheritdoc />
    public IGridRepository Grids { get; }

    /// <inheritdoc />
    public IAgvGridPositionRepository GridPositions { get; }

    /// <inheritdoc cref="EfStatisticsDb" />
    public EfStatisticsDb(IServiceProvider provider, StatisticsDbContext context, IMapRepository mapRepository, IGridRepository gridRepository, IAgvGridPositionRepository agvGridPositionRepository)
    {
        _efGridRepositoryRegistered = provider.GetService(typeof(EfGridRepository)) != null;
        _efMapRepositoryRegistered = provider.GetService(typeof(EfMapRepository)) != null;
        _efAgvGridPositionRepositoryRegistered = provider.GetService(typeof(EfAgvGridPositionRepository)) != null;
        DbContext = context;
        Maps = mapRepository;
        Grids = gridRepository;
        GridPositions = agvGridPositionRepository;
    }

    /// <inheritdoc cref="IAsyncDisposable" />
    [UsedImplicitly]
    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }
}