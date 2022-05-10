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
    public class WcsHostOrderConsumer:
        IConsumer<IHostOrderHandlingSucceeded>,
        IConsumer<IHostOrderStarted>,
        IConsumer<IHostOrderHandlingFailed>
    {
        private readonly IDbContextFactory<WcsDbContext> _contextFactory;
        private readonly ILogger<WcsHostOrderConsumer> _logger;

        /// <summary>
        /// Create WcsHostOrderConsumer
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        /// <param name="contextFactory"></param>
        [JetBrains.Annotations.PublicAPI]
        public WcsHostOrderConsumer(ILogger<WcsHostOrderConsumer> logger, IDbContextFactory<WcsDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Handle message <see cref="IHostOrderHandlingSucceeded"/> by writing to <see cref="FromAgv" /> table.
        /// An order with <see cref="IHostOrderHandlingSucceeded.OrderId"/> must be available in <see cref="ToAgv"/>.
        /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
        /// </summary>
        public async Task Consume(ConsumeContext<IHostOrderHandlingSucceeded> context)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);
            
            if ( dbContext.LatestOrderStatus(context.Message.OrderId) is { } order)
            {
                dbContext.FromAgv.Add(FromAgv.CreateInstance(order, OrderStatus.Finished, RecordType.TransportOrder));
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                _logger.LogError("Corresponding order with id {OrderId} could not be found in ToAgv", context.Message.OrderId);
            }
        }
        
        /// <summary>
        /// Handle message <see cref="IHostOrderStarted"/> by writing to <see cref="FromAgv" /> table.
        /// An order with <see cref="IHostOrderHandlingSucceeded.OrderId"/> must be available in <see cref="ToAgv"/>.
        /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
        /// </summary>
        public async Task Consume(ConsumeContext<IHostOrderStarted> context)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);
            
            if ( dbContext.LatestOrderStatus(context.Message.OrderId) is { } order)
            {
                dbContext.FromAgv.Add(
                    FromAgv.CreateInstance(order, OrderStatus.InProgress, RecordType.TransportOrder, vehicle: context.Message.AssignedAgv)
                    );
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                _logger.LogError("Corresponding order with id {OrderId} could not be found in ToAgv", context.Message.OrderId);
            }
        }
        
        /// <summary>
        /// Handle message <see cref="IHostOrderHandlingFailed"/> by writing to <see cref="FromAgv" /> table.
        /// An order with <see cref="IHostOrderHandlingSucceeded.OrderId"/> must be available in <see cref="ToAgv"/>.
        /// The <see cref="FromAgv" /> columns are filled with the information found in <see cref="ToAgv"/>.
        /// </summary>
        public async Task Consume(ConsumeContext<IHostOrderHandlingFailed> context)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);
            
            if ( dbContext.LatestOrderStatus(context.Message.OrderId) is { } order)
            {
                dbContext.FromAgv.Add(
                    FromAgv.CreateInstance(order, OrderStatus.Finished, RecordType.TransportOrder, error: context.Message.Error.ToString())
                    );
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                _logger.LogError("Corresponding order with id {OrderId} could not be found in ToAgv", context.Message.OrderId);
            }
        }
    }
}
