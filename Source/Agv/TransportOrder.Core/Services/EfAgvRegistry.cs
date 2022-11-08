using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Agv Registry using Entity Framework to access the AgvSaga persistence storage 
    /// </summary>
    public class EfAgvRegistry : IAgvRegistry
    {
        /// <summary>
        /// Get count of all AGVs in the registry
        /// </summary>
        public int Count => AgvSagas.Count();

        /// <summary>
        /// Get count of idle AGVs in the registry
        /// </summary>
        public int CountIdle => GetIdleAgvs().Count();

        private readonly IDbContextFactory<SagaPersistenceDbContext> _dbContextFactory;

        private IQueryable<AgvSaga> AgvSagas =>
            _dbContextFactory.CreateDbContext()
                .Set<AgvSaga>()
                .AsNoTracking(); // No need to track changes

        /// <summary>
        /// Constructor of the EF Agv Registry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        public EfAgvRegistry(IDbContextFactory<SagaPersistenceDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc/>
        public IQueryable<IAgv> GetAllAgvs()
        {
            return AgvSagas
                .Cast<IAgv>();
        }

        /// <inheritdoc/>
        public IQueryable<IAgv> GetIdleAgvs()
        {
            return AgvSagas.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))
                .Cast<IAgv>();
        }

        /// <inheritdoc/>
        public IAgv? GetAgv(Guid correlationId)
        {
            return AgvSagas
                .Where(x => x.CorrelationId == correlationId)
                .Cast<IAgv>()
                .FirstOrDefault();
        }
    }
}
