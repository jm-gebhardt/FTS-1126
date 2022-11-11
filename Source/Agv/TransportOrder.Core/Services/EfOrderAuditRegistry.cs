using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Order Audit Registry using Entity Framework to access the AgvSaga persistence storage
    /// </summary>
    public class EfOrderAuditRegistry : IOrderAuditRegistry
    {
        private readonly ILogger<EfOrderAuditRegistry> _logger;
        private readonly IDbContextFactory<SagaPersistenceDbContext> _dbContextFactory;

        /// <summary>
        /// Constructor of EfOrderAuditRegistry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="logger"></param>
        public EfOrderAuditRegistry(IDbContextFactory<SagaPersistenceDbContext> dbContextFactory, ILogger<EfOrderAuditRegistry> logger)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        private IQueryable<OrderChangedEvent> OrderAuditEntries =>
            _dbContextFactory.CreateDbContext()
                .Set<OrderChangedEvent>()
                .AsNoTracking();

        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public IEnumerable<OrderChangedEvent> GetAllOrderChangedEventEntries()
        {
            return OrderAuditEntries.AsEnumerable();
        }

        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public bool Exists(Guid correlationId)
        {
            var request = OrderAuditEntries.FirstOrDefault(q => q.CorrelationId == correlationId);
            return request != null;
        }

        /// <inheritdoc/>
        public async Task<bool> Add(OrderSaga orderSaga, string eventName, string state, Hook hook, CancellationToken cancellationToken)
        {
            try
            {
                // CancellationToken is necessary in order to prevent thread concurrency errors
                var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                dbContext.Add(new OrderChangedEvent(Guid.NewGuid(), orderSaga, eventName, state, hook.ToString(), DateTime.UtcNow));

                int stateEntries = await dbContext.SaveChangesAsync(cancellationToken);
                return stateEntries != 0;
            }
            catch (MassTransit.NotAcceptedStateMachineException e)
            {
                _logger.LogError("MassTransit.NotAcceptedStateMachineException occurred \n {Message} \n {StackTrace}", e.Message, e.StackTrace);
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError("{Message} \n {StackTrace}", e.Message, e.StackTrace);
                return false;
            }
        }
    }
}
