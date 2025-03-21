using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Repositories;

/// <summary>
/// Provides a repository for short loop we target station related queries-
/// </summary>
public interface IShortLoopWeTargetStationRepository: IRepository<ShortLoopWeTargetStation>
{
    /// <summary>
    /// Get the short loop WE target station.
    /// </summary>
    Task<ShortLoopWeTargetStation> GetTargetStation();

    /// <summary>
    /// Update the short loop WE target station.
    /// </summary>
    /// <param name="station">The updated station.</param>
    Task UpdateAsync(ShortLoopWeTargetStation station);
}