using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;

/// <summary>
/// Provides action related queries.
/// </summary>
public interface IActionRepository: IRepository<OrderIdMapping>
{
    /// <summary>
    /// Add mapping which order id was sent to the vehicle and the one used fleet management internally.
    /// </summary>
    /// <param name="vehicleOrderId">The order id send to the vehicle.</param>
    /// <param name="internalOrderId">The order id of the action used fleet management service internally.</param>
    Task AddVehicleAndInternalOrderIdMapping(Guid vehicleOrderId, Guid internalOrderId);

    /// <summary>
    /// Get the fleet management services internal order id corresponding to the one sent to the vehicle.
    /// </summary>
    /// <param name="vehicleOrderId">The order id send to the vehicle.</param>
    /// <returns>The order id used fleet management internally.</returns>
    Task<Guid?> GetInternalOrderId(Guid vehicleOrderId);

    /// <summary>
    /// Get the vehicle order id corresponding to the one used fleet management services internally.
    /// </summary>
    /// <param name="internalOrderId">The order id of the action used fleet management service internally.</param>
    /// <returns>The order id send to the vehicle.</returns>
    Task<Guid?> GetVehicleOrderId(Guid internalOrderId);

    /// <summary>
    /// Remove mapping which order id was sent to the vehicle and the one used fleet management internally.
    /// </summary>
    /// <param name="vehicleOrderId">The order id send to the vehicle.</param>
    Task RemoveVehicleAndInternalOrderIdMapping(Guid vehicleOrderId);
}