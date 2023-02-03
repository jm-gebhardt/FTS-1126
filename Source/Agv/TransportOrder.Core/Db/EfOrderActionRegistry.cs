using Microsoft.EntityFrameworkCore;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Db
{
    /// <summary>
    /// Repository to access order actions
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class EfOrderActionRegistry : IOrderActionRegistry
    {
        private readonly DbContext _context;
        private int _sequenceNumber;

        /// <summary>
        /// Constructor.
        /// </summary>
        public EfOrderActionRegistry(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
        {
            _context = dbContextFactory.CreateDbContext();
        }

        #region Dispose

        private bool _disposed;

        /// <summary>Dispose</summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>Dispose</summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion


        /// <inheritdoc cref="IOrderActionRegistry"/>
        public void Insert(OrderDetails order, ActionType type, string? source = null, string? destination = null)
        {
            _context.Add(new OrderAction(order, ++_sequenceNumber, type, source, destination));
        }

        /// <inheritdoc cref="IOrderActionRegistry"/>
        public OrderAction? GetActionById(Guid id)
        {
            return _context.Set<OrderAction>()
                .Find(id);
        }

        /// <inheritdoc cref="IOrderActionRegistry"/>
        public List<OrderAction> GetPendingActions()
        {
            return _context.Set<OrderAction>()
                .Where(a => a.State == OrderActionState.Created || a.State == OrderActionState.Assigned)
                .ToList();
        }

        /// <inheritdoc cref="IOrderActionRegistry"/>
        public List<OrderAction> GetActionsOfOrder(Guid orderCorrelationId)
        {
            return _context.Set<OrderAction>()
                .Where(a => a.OrderCorrelationId == orderCorrelationId)
                .ToList();
        }

        /// <inheritdoc cref="IOrderActionRegistry"/>
        public OrderAction? GetSubsequentAction(Guid orderCorrelationId)
        {
            return _context.Set<OrderAction>()
                .Where(a => a.OrderCorrelationId == orderCorrelationId && a.State == OrderActionState.Assigned)
                .OrderBy(a => a.SequenceNumber)
                .FirstOrDefault();
        }

        /// <inheritdoc cref="IOrderActionRegistry"/>
        public void DeleteOrder(Guid orderCorrelationId)
        {
            _context.Set<OrderAction>()
                .RemoveRange(
                    _context.Set<OrderAction>()
                        .Where(a => a.OrderCorrelationId == orderCorrelationId)
                );
        }

        /// <inheritdoc cref="IOrderActionRegistry"/>
        public void SaveChanges()
        {
            _context.SaveChanges();
            _sequenceNumber = 0;
        }
    }
}
