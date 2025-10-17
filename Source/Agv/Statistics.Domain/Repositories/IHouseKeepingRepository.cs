namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for housekeeping the database
/// </summary>
public interface IHouseKeepingRepository
{
    /// <summary>
    /// Compress statistics data.
    /// </summary>
    public Task CompressStatistics();
}