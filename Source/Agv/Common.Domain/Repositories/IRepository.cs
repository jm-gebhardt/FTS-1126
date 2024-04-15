using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Domain.Repositories;

/// <summary>
/// Provides a marker interface for repositories.
/// </summary>
public interface IRepository;

/// <summary>
/// Provides a generic marker interface for repositories.
/// </summary>
public interface IRepository<T> : IRepository where T : class
{
    /// <summary>
    /// Gets all aggregates.
    /// </summary>
    /// <returns></returns>
    Task<ICollection<T>> GetAll();

    /// <summary>
    /// Get one by id.
    /// </summary>
    /// <param name="id">The aggregate id.</param>
    /// <returns></returns>
    Task<T> GetByIdAsync(Guid id);

    /// <summary>
    /// Removes an aggregate.
    /// </summary>
    /// <param name="aggregate"></param>
    [PublicAPI]
    void Remove(T aggregate);

    /// <summary>
    /// Adds an aggregate.
    /// </summary>
    /// <param name="aggregate"></param>
    void Add(T aggregate);

    /// <summary>
    /// Adds multiple aggregates.
    /// </summary>
    /// <param name="aggregates"></param>
    [PublicAPI]
    void AddRange(IEnumerable<T> aggregates);

    /// <summary>
    /// Adds multiple aggregates.
    /// </summary>
    /// <param name="aggregates"></param>
    [PublicAPI]
    void AddRange(params T[] aggregates);

    /// <summary>
    /// Save the changes.
    /// </summary>
    /// <returns></returns>
    Task SaveAsync();
}