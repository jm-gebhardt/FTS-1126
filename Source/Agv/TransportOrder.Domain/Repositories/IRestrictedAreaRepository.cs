using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for restricted area related queries.
/// </summary>
public interface IRestrictedAreaRepository
{
    /// <summary>
    /// True, if a point is in a restricted area.
    /// </summary>
    /// <param name="x">The x coordinate of the point.</param>
    /// <param name="y">The y coordinate of the point.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsPointInRestrictedArea(float x, float y, CancellationToken cancellationToken=default);

    /// <summary>
    /// Add a restricted area.
    /// </summary>
    /// <param name="area">The restricted area.</param>
    Task AddRestrictedArea(RestrictedArea area);

    /// <summary>
    /// Update a restricted area.
    /// </summary>
    /// <param name="area">The restricted area.</param>
    Task UpdateRestrictedArea(RestrictedArea area);

    /// <summary>
    /// Get a restricted area by its id.
    /// </summary>
    /// <param name="id">The id of the restricted area.</param>
    /// <returns>The restricted area</returns>
    /// <exception cref="RestrictedAreaNotFoundException">Thrown, if no restricted area was found that has the given id.</exception>
    Task<RestrictedArea> GetRestrictedArea(Guid id);

    /// <summary>
    /// Get the restricted ares for the given ids (all none is given).
    /// </summary>
    /// <param name="ids">The ids of the restricted areas</param>
    /// <returns>Collection of restricted areas that have the given ids.</returns>
    /// <exception cref="RestrictedAreaNotFoundException">Thrown, if any of the restricted areas was found using the given ids.</exception>
    Task<ICollection<RestrictedArea>> GetRestrictedAreas(Guid[] ids);

    /// <summary>
    /// Delete restricted area given by its id.
    /// </summary>
    /// <param name="id">The id of the restricted area</param>
    /// <exception cref="RestrictedAreaNotFoundException">Thrown, if no restricted area was found that has the given id.</exception>
    Task DeleteRestrictedArea(Guid id);
}