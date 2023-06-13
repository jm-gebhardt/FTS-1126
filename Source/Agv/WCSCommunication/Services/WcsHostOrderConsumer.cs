using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db.Models;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services
{
    /// <summary>
    /// Consumes HostOrder related messages from TransportOrder
    /// </summary>
    public class WcsHostOrderConsumer :
        IConsumer<HostOrderHandlingSucceeded>,
        IConsumer<HostOrderStarted>,
        IConsumer<HostOrderHandlingFailed>,
        IConsumer<HostOrderCancelSucceeded>,
        IConsumer<HostOrderCancelFailed>
    {
        private readonly IDbContextFactory<WcsHostDbContext> _contextFactory;
        private readonly ILogger<WcsHostOrderConsumer> _logger;

        /// <summary>
        /// Create WcsHostOrderConsumer
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        /// <param name="contextFactory"></param>
        [JetBrains.Annotations.PublicAPI]
        public WcsHostOrderConsumer(ILogger<WcsHostOrderConsumer> logger, IDbContextFactory<WcsHostDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Handle message <see cref="HostOrderHandlingSucceeded"/> by writing to <see cref="FromAgv" /> table.
        /// An order with <see cref="HostOrderHandlingSucceeded.OrderId"/> must be available in <see cref="ToAgv"/>.
        /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
        /// </summary>
        public async Task Consume(ConsumeContext<HostOrderHandlingSucceeded> context)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);

            if (dbContext.LatestOrderStatus(context.Message.OrderId) is { } order)
            {
                dbContext.FromAgv.Add(FromAgv.CreateInstance(order, OrderStatus.Finished, RecordType.TransportOrder));
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                _logger.LogError("Corresponding order with id {OrderId} / {CorrelationId} could not be found in ToAgv",
                    context.Message.OrderId, context.Message.CorrelationId);
            }
        }

        /// <summary>
        /// Handle message <see cref="HostOrderStarted"/> by writing to <see cref="FromAgv" /> table.
        /// An order with <see cref="HostOrderHandlingSucceeded.OrderId"/> must be available in <see cref="ToAgv"/>.
        /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
        /// </summary>
        public async Task Consume(ConsumeContext<HostOrderStarted> context)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);

            if (dbContext.LatestOrderStatus(context.Message.OrderId) is { } order)
            {
                dbContext.FromAgv.Add(
                    FromAgv.CreateInstance(order, OrderStatus.InProgress, RecordType.TransportOrder, vehicle: context.Message.AssignedAgv)
                    );
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                _logger.LogError("Corresponding order with id {OrderId} / {CorrelationId} could not be found in ToAgv",
                    context.Message.OrderId, context.Message.CorrelationId);
            }
        }

        /// <summary>
        /// Handle message <see cref="HostOrderHandlingFailed"/> by writing to <see cref="FromAgv" /> table.
        /// An order with <see cref="HostOrderHandlingSucceeded.OrderId"/> must be available in <see cref="ToAgv"/>.
        /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
        /// </summary>
        public async Task Consume(ConsumeContext<HostOrderHandlingFailed> context)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);

            if (dbContext.LatestOrderStatus(context.Message.OrderId) is { } order)
            {
                dbContext.FromAgv.Add(
                    FromAgv.CreateInstance(order, OrderStatus.Finished, RecordType.TransportOrder, error: context.Message.Error.ToString())
                    );
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                _logger.LogError("Corresponding order with id {OrderId} / {CorrelationId} could not be found in ToAgv",
                    context.Message.OrderId, context.Message.CorrelationId);
            }
        }

        /// <summary>
        /// Respond to <see cref="HostOrderCancelSucceeded"/>
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<HostOrderCancelSucceeded> context)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);

            if (dbContext.LatestOrderStatus(context.Message.OrderId) is { } order)
            {
                dbContext.FromAgv.Add(
                    FromAgv.CreateInstance(order, OrderStatus.Cancelled, RecordType.OrderCancelAcknowledge));
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                _logger.LogError("Corresponding order with id {OrderId} / {CorrelationId} could not be found in ToAgv",
                    context.Message.OrderId, context.Message.CorrelationId);
            }
        }

        /// <summary>
        /// Respond to <see cref="HostOrderCancelFailed"/>
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<HostOrderCancelFailed> context)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);

            if (dbContext.LatestOrderStatus(context.Message.OrderId) is { } order)
            {
                dbContext.FromAgv.Add(
                    FromAgv.CreateInstance(order, OrderStatus.Cancelled, RecordType.OrderCancelFailed, error: context.Message.Error.ToString()));
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                _logger.LogError("Corresponding order with id {OrderId} / {CorrelationId} could not be found in ToAgv",
                    context.Message.OrderId, context.Message.CorrelationId);
            }
        }
    }
}
