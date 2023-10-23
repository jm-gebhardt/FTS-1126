using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.DbInterface.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.DbInterface;

/// <summary>
/// Repository service for communicating with the wcs host via
/// FromAgv/ToAgv database tables.
/// </summary>
[UsedImplicitly]
public class WcsDbCommunicator: IHostCommunicator
{
    private readonly IDbContextFactory<WcsHostDbContext> _contextFactory;
    
    /// <inheritdoc cref="WcsDbCommunicator"/>
    public WcsDbCommunicator(IDbContextFactory<WcsHostDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Handle succeeded host order by writing to <see cref="FromAgv" /> table.
    /// An order with <see param="orderId"/> must be available in <see cref="ToAgv"/>.
    /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
    /// </summary>
    public async Task<bool> TransmitHostOrderHandlingSucceeded(string orderId)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var order = dbContext.LatestOrderStatus(orderId);

        if (order == null)
        {
            throw new OrderNotFoundException(orderId);
        }
        dbContext.FromAgv.Add(FromAgv.CreateInstance(order, OrderStatus.Finished, RecordType.TransportOrder));
        
        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <summary>
    /// Handle host order start by writing to <see cref="FromAgv" /> table.
    /// An order with <see parm="orderId"/> must be available in <see cref="ToAgv"/>.
    /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
    /// </summary>
    public async Task<bool> TransmitHostOrderStarted(string orderId, string assignedAgv)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var order = dbContext.LatestOrderStatus(orderId);

        if (order == null)
        {
            throw new OrderNotFoundException(orderId);
        }

        dbContext.FromAgv.Add(
            FromAgv.CreateInstance(order, OrderStatus.InProgress, RecordType.TransportOrder, vehicle: assignedAgv)
        );
        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <summary>
    /// Handle failure of host order by writing to <see cref="FromAgv" /> table.
    /// An order with <see param="orderId"/> must be available in <see cref="ToAgv"/>.
    /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
    /// </summary>
    public async Task<bool> TransmitHostOrderHandlingFailure(string orderId, string error)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var order = dbContext.LatestOrderStatus(orderId);

        if (order == null)
        {
            throw new OrderNotFoundException(orderId);
        }

        dbContext.FromAgv.Add(
            FromAgv.CreateInstance(order, OrderStatus.Finished, RecordType.TransportOrder, error: error)
        );
        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public async Task<bool> TransmitHostOrderCancellationFailure(string orderId, string error)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var order = dbContext.LatestOrderStatus(orderId);

        if (order == null)
        {
            throw new OrderNotFoundException(orderId);
        }
        dbContext.FromAgv.Add(
            FromAgv.CreateInstance(order, OrderStatus.Cancelled, RecordType.OrderCancelFailed, error: error));
        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public async Task<bool> TransmitHostOrderCancellationSucceeded(string orderId)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var order = dbContext.LatestOrderStatus(orderId);

        if (order == null)
        {
            throw new OrderNotFoundException(orderId);
        }

        dbContext.FromAgv.Add(
            FromAgv.CreateInstance(order, OrderStatus.Cancelled, RecordType.OrderCancelAcknowledge));
        return await dbContext.SaveChangesAsync() > 0;
    }
}