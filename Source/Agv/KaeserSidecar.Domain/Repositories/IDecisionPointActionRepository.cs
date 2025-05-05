using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides a repository for decision action related requests.
/// </summary>
public interface IDecisionPointActionRepository : IRepository<DecisionPointAction>
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
    Task UpdateAsync(DecisionPointAction action);
}