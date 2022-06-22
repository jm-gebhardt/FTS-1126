using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services;

/// <summary>
/// 
/// </summary>
public class WcsOrderHandler: IPollable
{
    private readonly ILogger<WcsOrderHandler> _logger;
    private readonly IBus _bus;
    private readonly IDbContextFactory<WcsDbContext> _contextFactory;

    /// <summary>
    /// Handles all actions found in the ToAgv table
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="bus"></param>
    /// <param name="contextFactory"></param>
    public WcsOrderHandler(ILogger<WcsOrderHandler> logger, IBus bus, IDbContextFactory<WcsDbContext> contextFactory)
    {
        _logger = logger;
        _bus = bus;
        _contextFactory = contextFactory;
        EnsureDbCreated();
    }

    /// <summary>
    /// Ensure that the Database has been created
    /// This can be removed when migrations are added
    /// </summary>
    private void EnsureDbCreated()
    {
         using var context = _contextFactory.CreateDbContext();
         context.Database.EnsureCreated();
    }

    /// <summary>
    /// Handle new transport orders where RecordType == RecordType.TransportOrder
    /// </summary>
    /// <param name="cancellationToken"></param>
    [JetBrains.Annotations.PublicAPI]
    public async Task HandleNewTransportOrders(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        foreach (var order in context.NewOrders)
        {
            if (!order.IsValid)
            {
                _logger.LogError("Found invalid order in ToAgv with id={OrderId}", order.Id);
                order.MessageStatus = MessageStatus.Failed; // Mark Message as Failed
            }
            else
            {
                var correlationId = order.OrderId.ToGuid();
                _logger.LogInformation("New order in FromAgv: {OrderId} / {CorrelationId}", order.OrderId, correlationId);
                await _bus.Publish<IHandleHostOrder>(new
                {
                    Order = new Order(
                        correlationId,
                        order.OrderId,
                        order.Source,
                        order.Destination,
                        order.LeNo,
                        HostOrderCreator.Wms
                    )
                }, cancellationToken);
                context.FromAgv.Add(FromAgv.CreateInstance(order, OrderStatus.Pending, RecordType.TransportOrder));
                order.MessageStatus = MessageStatus.Finished; // Mark Message as Read
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Poll database for new actions 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task Poll(CancellationToken cancellationToken = default)
    {
        await HandleNewTransportOrders(cancellationToken);
    }
}