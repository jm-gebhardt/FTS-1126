using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs
{
    /// <summary>
    /// Map Registry using Entity Framework to access the AgvSaga persistence storage
    /// </summary>
    public class EfAgvDataRegistry : IAgvDataRegistry
    {
        private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

        private IQueryable<AgvData> AgvData => _dbContextFactory.CreateDbContext().Set<AgvData>().AsNoTracking();

        /// <summary>
        /// Constructor of the EF AgvDataRegistry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        public EfAgvDataRegistry(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc/>
        public bool Exists(Guid correlationId)
        {
            return AgvData.Any(q => q.CorrelationId == correlationId);
        }

        /// <inheritdoc/>
        public AgvData? GetAgvData(Guid correlationId)
        {
            return AgvData.FirstOrDefault(q => q.CorrelationId == correlationId);
        }

        /// <inheritdoc/>
        public bool AddOrUpdate(AgvData agvData)
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            DbSet<AgvData> dbSet = dbContext.Set<AgvData>();

            if (dbSet.Any(a => a.CorrelationId == agvData.CorrelationId))
            {
                dbSet.Update(agvData);
            }
            else
            {
                dbSet.Add(agvData);
            }

            return dbContext.SaveChanges() > 0;
        }
    }
}