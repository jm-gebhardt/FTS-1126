using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Abstracted interface for AgvDataRegistry
/// </summary>
public interface IAgvDataRegistry
{
    /// <summary>
    /// Checks if AgvData related to its correlationId exists in registry
    /// </summary>
    /// <param name="correlationId">GUID of the AgvData looked for in the registry</param>
    public bool Exists(Guid correlationId);

    /// <summary>
    /// Gets the AgvData for an AGV from the AgvDataRegistry
    /// </summary>
    /// <param name="correlationId">AGV correlationId</param>
    /// <returns><see cref="AgvData"/> object or null if no data was found for the given correlationId</returns>
    public AgvData? GetAgvData(Guid correlationId);

    /// <summary>
    /// Add or update a AgvData to/in the registry
    /// </summary>
    /// <param name="agvData">AgvData</param>
    /// <returns>True if a change has been made</returns>
    public Task<bool> AddOrUpdateAsync(AgvData agvData);
}