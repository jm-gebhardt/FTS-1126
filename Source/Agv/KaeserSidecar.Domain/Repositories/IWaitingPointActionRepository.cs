using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides a repository for waiting action related requests.
/// </summary>
public interface IWaitingPointActionRepository: IRepository<WaitingPointAction>
{
    /// <summary>
    /// Remove an action.
    /// </summary>
    /// <param name="actionCorrelationId">The action correlation id.</param>
    Task RemoveById(Guid actionCorrelationId);

    /// <summary>
    /// Update an action.
    /// </summary>
    /// <param name="action">The action to update.</param>
    Task UpdateAsync(WaitingPointAction action);
}