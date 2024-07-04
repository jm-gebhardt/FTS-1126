using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.HostOrder.Commands;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.WCSCommunication.Messages.Commands;
using Gebhardt.Acc.WCSCommunication.WcsApi.DbInterface.Models;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.WCSCommunication.WcsApi.DbInterface;

/// <summary>
/// Service that checks database for new host orders
/// </summary>
public class WcsOrderHandler : IPollable
{
    private readonly ILogger<WcsOrderHandler> _logger;
    private readonly IBus _bus;
    private readonly IDbContextFactory<WcsHostDbContext> _contextFactory;

    /// <summary>
    /// Handles all actions found in the ToAgv table
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="bus"></param>
    /// <param name="contextFactory"></param>
    public WcsOrderHandler(ILogger<WcsOrderHandler> logger, IBus bus, IDbContextFactory<WcsHostDbContext> contextFactory)
    {
        _logger = logger;
        _bus = bus;
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Poll database for new actions 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task Poll(CancellationToken cancellationToken = default)
    {
        await HandleNewTransportOrders(cancellationToken);
        await HandleIncomingCancelRequests(cancellationToken);
    }

    /// <summary>
    /// Handle new transport orders where RecordType == RecordType.TransportOrder
    /// </summary>
    /// <param name="cancellationToken"></param>
    [PublicAPI]
    public async Task HandleNewTransportOrders(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        foreach (var order in context.NewOrders)
        {
            if (!order.IsValid)
            {
                _logger.LogError("Found invalid order in ToAgv with OrderId = {OrderId}", order.Id);
                order.MessageStatus = MessageStatus.Failed; // Mark Message as FailedDate
            }
            else
            {
                var correlationId = order.OrderId.ToGuid();

                _logger.LogInformation("New order in ToAgv: {OrderId} / {CorrelationId}", order.OrderId, correlationId);

                var hostOrderDetails = new OrderDetails(correlationId, order.OrderId, order.Source, order.Destination, order.LeNo, HostOrderCreator.Wms, order.Created);
                await _bus.Publish(new CreateHostOrder(hostOrderDetails), cancellationToken);

                context.FromAgv.Add(FromAgv.CreateInstance(order, OrderStatus.Pending, RecordType.TransportOrder));
                order.MessageStatus = MessageStatus.Finished; // Mark Message as Read
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Handle incoming orders to be canceled with RecordType == RecordType.CancelRequest
    /// </summary>
    /// <param name="cancellationToken"></param>
    [PublicAPI]
    public async Task HandleIncomingCancelRequests(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        foreach (var order in context.CancelRequests)
        {
            if (!order.IsValid)
            {
                _logger.LogError("Found invalid order in ToAgv with OrderId = {OrderId} / {CorrelationId}",
                    order.Id, order.OrderId.ToGuid());
                order.MessageStatus = MessageStatus.Failed; // Mark Message as FailedDate
            }
            else
            {
                await _bus.Publish(new CancelHostOrder(order.OrderId.ToGuid(), order.OrderId), cancellationToken);

                _logger.LogInformation("Cancel request for order {OrderId} / {CorrelationId} has been sent",
                    order.OrderId, order.OrderId.ToGuid());

                context.FromAgv.Add(FromAgv.CreateInstance(order, OrderStatus.Canceled, RecordType.OrderCancelRequest));
                order.MessageStatus = MessageStatus.Finished;
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}