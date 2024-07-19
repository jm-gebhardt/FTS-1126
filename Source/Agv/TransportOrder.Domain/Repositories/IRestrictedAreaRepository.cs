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
    Task<bool> IsPointInRestrictedArea(float x, float y);
}