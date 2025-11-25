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
    private readonly bool _efGridCellRepositoryRegistered;
    private readonly bool _efGridCellStayRepositoryRegistered;
    private readonly bool _efMapGridCelRepositoryRegistered;
    private readonly bool _efAgvGridErrorRepositoryRegistered;
    private readonly bool _efAgvGridFieldViolationRepositoryRegistered;
    private readonly bool _efAgvWifiMetricsRepositoryRegistered;

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

        if (!_efGridCellRepositoryRegistered)
        {
            await GridCell.SaveAsync();
        }

        if (!_efGridCellStayRepositoryRegistered)
        {
            await GridCellStay.SaveAsync();
        }

        if (!_efMapGridCelRepositoryRegistered)
        {
            await MapGridCell.SaveAsync();
        }

        if (!_efAgvGridErrorRepositoryRegistered)
        {
            await GridAgvErrors.SaveAsync();
        }

        if (!_efAgvGridFieldViolationRepositoryRegistered)
        {
            await GridAgvFieldViolations.SaveAsync();
        }

        if (!_efAgvWifiMetricsRepositoryRegistered)
        {
            await GridAgvWifiMetrics.SaveAsync();
        }

        return await DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IMapRepository Maps { get; }

    /// <inheritdoc />
    public IGridRepository Grids { get; }

    /// <inheritdoc />
    public IGridCellRepository GridCell { get; }

    /// <inheritdoc />
    public IGridCellStayRepository GridCellStay { get; }

    /// <inheritdoc />
    public IMapGridCellRepository MapGridCell { get; }

    /// <inheritdoc />
    public IAgvErrorRepository GridAgvErrors { get; }

    /// <inheritdoc />
    public IAgvFieldViolationRepository GridAgvFieldViolations { get; }

    /// <inheritdoc />
    public IAgvWifiMetricsRepository GridAgvWifiMetrics { get; }

    /// <inheritdoc cref="EfStatisticsDb" />
    public EfStatisticsDb(IServiceProvider provider,
        StatisticsDbContext context,
        IMapRepository mapRepository,
        IGridRepository gridRepository,
        IGridCellRepository gridCellRepository,
        IGridCellStayRepository gridCellStayRepository,
        IMapGridCellRepository mapGridCellRepository,
        IAgvErrorRepository gridAgvErrorRepository,
        IAgvFieldViolationRepository gridAgvFieldViolationRepository,
        IAgvWifiMetricsRepository gridAgvWifiMetricsRepository
    )
    {
        _efGridRepositoryRegistered = provider.GetService(typeof(EfGridRepository)) != null;
        _efMapRepositoryRegistered = provider.GetService(typeof(EfMapRepository)) != null;
        _efGridCellRepositoryRegistered = provider.GetService(typeof(EfGridCellRepository)) != null;
        _efGridCellStayRepositoryRegistered = provider.GetService(typeof(EfGridCellStayRepository)) != null;
        _efMapGridCelRepositoryRegistered = provider.GetService(typeof(EfMapGridCellRepository)) != null;
        _efAgvGridErrorRepositoryRegistered = provider.GetService(typeof(EfAgvErrorRepository)) != null;
        _efAgvGridFieldViolationRepositoryRegistered = provider.GetService(typeof(EfAgvFieldViolationRepository)) != null;
        _efAgvWifiMetricsRepositoryRegistered = provider.GetService(typeof(EfAgvWifiMetricsRepository)) != null;
        DbContext = context;
        Maps = mapRepository;
        Grids = gridRepository;
        GridCell = gridCellRepository;
        GridCellStay = gridCellStayRepository;
        MapGridCell = mapGridCellRepository;
        GridAgvErrors = gridAgvErrorRepository;
        GridAgvFieldViolations = gridAgvFieldViolationRepository;
        GridAgvWifiMetrics = gridAgvWifiMetricsRepository;
    }

    /// <inheritdoc cref="IAsyncDisposable" />
    [UsedImplicitly]
    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }
}