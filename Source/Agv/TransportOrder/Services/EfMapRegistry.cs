using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Map Registry using Entity Framework to access the AgvSaga persistence storage
    /// </summary>
    public class EfMapRegistry : IMapRegistry
    {
        private readonly SagaPersistenceDbContext _dbContext;
        private readonly ILogger<EfMapRegistry> _logger;

        /// <summary>
        /// Constructor of the EF MapRegistry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="logger"></param>
        public EfMapRegistry(IDbContextFactory<SagaPersistenceDbContext> dbContextFactory, ILogger<EfMapRegistry> logger)
        {
            _dbContext = dbContextFactory.CreateDbContext();
            _logger = logger;
        }

        private IQueryable<Map> Maps =>
            _dbContext
                .Set<Map>()
                .AsNoTracking(); // No need to track changes

        /// <inheritdoc/>
        public IEnumerable<Map> GetAllMaps()
        {
            return Maps.AsEnumerable();
        }

        /// <inheritdoc/>
        public byte[]? GetMap(Guid correlationId)
        {
            return Maps.First(q => q.CorrelationId == correlationId).MapValue;
        }

        /// <inheritdoc/>
        public bool Exists(Guid correlationId)
        {
            var request = Maps.FirstOrDefault(q => q.CorrelationId == correlationId);
            return request != null;
        }

        /// <inheritdoc/>
        public bool Add(Guid correlationId, string base64PngImage, string mapId)
        {
            try
            {
                _dbContext.Add(new Map(correlationId, mapId, Convert.FromBase64String(base64PngImage), DateTime.Now));

                int stateEntries = _dbContext.SaveChanges();
                return stateEntries != 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool Update(Guid correlationId, string base64PngImage)
        {
            try
            {
                var map = _dbContext.Set<Map>().First(q => q.CorrelationId == correlationId);
                map.MapValue = Convert.FromBase64String(base64PngImage);
                map.ModificationDateTime = DateTime.Now;

                int stateEntries = _dbContext.SaveChanges();
                return stateEntries != 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return false;
            }
        }
    }
}
