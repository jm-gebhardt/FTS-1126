using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides a repository for telegram related queries.
/// </summary>
public interface ITelegramRepository
{
    /// <summary>
    /// Gets all telegrams.
    /// </summary>
    Task<ICollection<Telegram>> GetAll();

    /// <summary>
    /// Removes a telegram.
    /// </summary>
    /// <param name="aggregate">The telegram.</param>
    Task RemoveAsync(Telegram aggregate);

    /// <summary>
    /// Adds a telegram.
    /// </summary>
    /// <param name="aggregate">The telegram.</param>
    Task AddAsync(Telegram aggregate);

    /// <summary>
    /// Get one by id.
    /// </summary>
    /// <param name="id">The telegram id.</param>
    /// <returns></returns>
    Task<Telegram> GetByIdAsync(Guid id);
}