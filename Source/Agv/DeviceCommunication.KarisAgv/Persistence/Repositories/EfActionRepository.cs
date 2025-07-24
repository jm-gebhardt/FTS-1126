using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;

/// <inheritdoc cref="IActionRepository"/>>
public class EfActionRepository(DeviceCommunicationDbContext context) : EfRepository<OrderIdMapping, DeviceCommunicationDbContext>(context), IActionRepository
{
    /// <inheritdoc cref="IActionRepository"/>>
    public async Task AddVehicleAndInternalOrderIdMapping(Guid vehicleOrderId, Guid internalOrderId)
    {
        OrderIdMapping orderIdMapping;
        try
        {
            orderIdMapping = await GetByIdAsync(vehicleOrderId);
            orderIdMapping.InternalOrderId = internalOrderId;
        }
        catch (InvalidOperationException)
        {
            orderIdMapping = new OrderIdMapping(vehicleOrderId, internalOrderId);
            Add(orderIdMapping);
        }
        await SaveAsync();
    }

    /// <inheritdoc cref="IActionRepository"/>>
    public async Task<Guid?> GetInternalOrderId(Guid vehicleOrderId)
    {
        try
        {
            var orderIdMapping = await GetByIdAsync(vehicleOrderId);
            return orderIdMapping.InternalOrderId;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <inheritdoc cref="IActionRepository"/>>
    public async Task<Guid?> GetVehicleOrderId(Guid internalOrderId)
    {
        return (await Context.Set<OrderIdMapping>().SingleOrDefaultAsync(o => o.InternalOrderId == internalOrderId))?.VehicleOrderId;
    }

    /// <inheritdoc cref="IActionRepository"/>>
    public async Task RemoveVehicleAndInternalOrderIdMapping(Guid vehicleOrderId)
    {
        try
        {
            var orderIdMapping = await GetByIdAsync(vehicleOrderId);
            Remove(orderIdMapping);
            await SaveAsync();
        }
        catch (InvalidOperationException)
        {
            // do nothing
        }
    }
}