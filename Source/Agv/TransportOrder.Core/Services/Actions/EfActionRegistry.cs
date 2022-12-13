using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions
{
    /// <inheritdoc />
    public class EfActionRegistry : IActionRegistry
    {
        private readonly IDbContextFactory<SagaPersistenceDbContext> _dbContextFactory;

        /// <summary>
        /// Constructor of the Action Registry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        public EfActionRegistry(IDbContextFactory<SagaPersistenceDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc />
        public IList<AgvAction> GetAllPendingActions()
        {
            using var context = _dbContextFactory.CreateDbContext();

            return context
                .Set<ActionSaga>()
                .AsNoTracking()
                .Where(a => a.CurrentState == nameof(ActionStateMachine.ActionCreated))
                .Select(a => a.Action)
                .ToList();
        }
    }
}
