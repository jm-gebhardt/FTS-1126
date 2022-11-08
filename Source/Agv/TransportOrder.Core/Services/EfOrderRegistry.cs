using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Order Registry using Entity Framework to access the OrderSaga persistence storage 
    /// </summary>
    public class EfOrderRegistry : IOrderRegistry
    {
        private readonly IDbContextFactory<SagaPersistenceDbContext> _dbContextFactory;

        private IQueryable<OrderSaga> OrderSagas =>
            _dbContextFactory.CreateDbContext()
                .Set<OrderSaga>()
                .AsNoTracking(); // No need to track changes

        /// <summary>
        /// Constructor of the EF Order Registry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        public EfOrderRegistry(IDbContextFactory<SagaPersistenceDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc/>
        public int Count => OrderSagas.Count();

        /// <inheritdoc/>
        public int CountUnassigned => GetUnassignedOrders().Count();

        /// <inheritdoc/>
        public IQueryable<IOrder> GetAllOrders()
        {
            return OrderSagas
                .Cast<IOrder>();
        }

        /// <inheritdoc/>
        public IQueryable<IOrder> GetUnassignedOrders()
        {
            return OrderSagas.Where(x => x.CurrentState == nameof(OrderStateMachine.OrderReceived))
                .OrderBy(x => x.OrderDetails.CreationDate)
                .Cast<IOrder>();
        }

        /// <inheritdoc/>
        public IOrder? GetOrder(Guid correlationId)
        {
            return OrderSagas
                .Where(x => x.CorrelationId == correlationId)
                .Cast<IOrder>()
                .FirstOrDefault();
        }
    }
}
