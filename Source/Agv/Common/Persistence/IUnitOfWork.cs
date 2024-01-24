namespace Gebhardt.StoreWare.Agv.Common.Persistence;

/// <summary>
/// Provides a unit-of-work to coordinate changes over multiple repositories.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    /// <returns></returns>
    Task<int> SaveAsync(CancellationToken cancellationToken = default);
}
