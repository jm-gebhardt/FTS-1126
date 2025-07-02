using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

/// <summary>
/// Provides a test fixture for the <see cref="IStatisticsDb"/>.
/// </summary>
public class StatisticsDbTestFixture : DbTestFixtureBase<StatisticsDbContext>, IStatisticsDb, IDisposable
{
    private readonly StatisticsDbContext _context;

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public IMapRepository Maps { get; }

    public IGridRepository Grids { get; }

    public IGridAgvPositionRepository GridAgvPositions { get; }

    public IMapAgvPositionsRepository MapAgvPositions { get; }

    public IAgvErrorRepository GridAgvErrors { get; }

    public IAgvFieldViolationRepository GridAgvFieldViolations { get; }

    public IAgvWifiMetricsRepository GridAgvWifiMetrics { get; }

    /// <inheritdoc cref="StatisticsDbTestFixture"/>
    public StatisticsDbTestFixture()
    {
        _context = ContextFactory.CreateDbContext();
        Maps = new EfMapRepository(_context);
        Grids = new EfGridRepository(_context);
        GridAgvPositions = new EfGridAgvPositionRepository(_context);
        MapAgvPositions = new EfMapAgvPositionsRepository(_context);
        GridAgvErrors = new EfAgvErrorRepository(_context);
        GridAgvFieldViolations = new EfAgvFieldViolationRepository(_context);
        GridAgvWifiMetrics = new EfAgvWifiMetricsRepository(_context);
    }
}