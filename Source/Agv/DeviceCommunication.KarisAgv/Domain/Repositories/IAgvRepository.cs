using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;

/// <summary>
/// Provides agv related queries.
/// </summary>
public interface IAgvRepository : IRepository<Agv>
{
    /// <summary>
    /// Stores the agv serial number.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="serialNumber">The serial number of the agv.</param>
    Task AddOrUpdate(Guid correlationId, string serialNumber);

    /// <summary>
    /// Get an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    Task<Agv> GetAgv(Guid correlationId);

    /// <summary>
    /// Load cached data.
    /// </summary>
    Task LoadCache();
}
    
