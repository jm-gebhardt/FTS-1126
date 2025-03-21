using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Repositories;

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
    Task<ICollection<T>> GetAll();

    /// <summary>
    /// Get one by id.
    /// </summary>
    /// <param name="id">The aggregate id.</param>
    Task<T> GetByIdAsync(Guid id);

    /// <summary>
    /// Removes an aggregate.
    /// </summary>
    [PublicAPI]
    void Remove(T aggregate);

    /// <summary>
    /// Removes an aggregate.
    /// </summary>
    [PublicAPI]
    Task RemoveAsync(T aggregate);

    /// <summary>
    /// Adds an aggregate.
    /// </summary>
    void Add(T aggregate);

    /// <summary>
    /// Adds an aggregate.
    /// </summary>
    Task AddAsync(T aggregate);

    /// <summary>
    /// Adds multiple aggregates.
    /// </summary>
    [PublicAPI]
    void AddRange(IEnumerable<T> aggregates);

    /// <summary>
    /// Adds multiple aggregates.
    /// </summary>
    [PublicAPI]
    void AddRange(params T[] aggregates);

    /// <summary>
    /// Save the changes.
    /// </summary>
    Task SaveAsync();
}