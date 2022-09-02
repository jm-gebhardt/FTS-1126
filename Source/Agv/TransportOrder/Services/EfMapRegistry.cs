using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Map Registry using Entity Framework to access the AgvSaga persistence storage
    /// </summary>
    public class EfMapRegistry : IMapRegistry
    {
        private readonly IDbContextFactory<SagaPersistenceDbContext> _dbContextFactory;
        private readonly ILogger<EfMapRegistry> _logger;

        /// <summary>
        /// Constructor of the EF MapRegistry
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="logger"></param>
        public EfMapRegistry(IDbContextFactory<SagaPersistenceDbContext> dbContextFactory, ILogger<EfMapRegistry> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        private IQueryable<Map> Maps =>
            _dbContextFactory.CreateDbContext()
                .Set<Map>()
                .AsNoTracking(); // No need to track changes

        /// <inheritdoc/>
        public IQueryable<Map> GetAllMaps()
        {
            return Maps;
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
        public bool Add(Guid correlationId, string mapId, string base64PngImage, double resolution, double xOffset, double yOffset, DateTime modificationDate)
        {
            try
            {
                var dbContext = _dbContextFactory.CreateDbContext(); 
                dbContext
                    .Add(new Map(
                    correlationId,
                    mapId,
                    Convert.FromBase64String(base64PngImage),
                    resolution,
                    xOffset,
                    yOffset,
                    modificationDate
                    ));

                int stateEntries = dbContext.SaveChanges();
                return stateEntries != 0;
            }
            catch (Exception e)
            {
                _logger.LogError("Could not add map: {Exception}", e.Message);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool Update(Guid correlationId,  string mapId, string base64PngImage, double resolution, double xOffset, double yOffset, DateTime modificationDate)
        {
            try
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                var map = dbContext
                    .Set<Map>()
                    .First(q => q.CorrelationId == correlationId);
                map.MapId = mapId;
                map.MapValue = Convert.FromBase64String(base64PngImage);
                map.Resolution = resolution;
                map.XOffset = xOffset;
                map.YOffset = yOffset;
                map.ModificationDate = modificationDate;

                int stateEntries = dbContext.SaveChanges();
                return stateEntries != 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not update map");
                return false;
            }
        }
    }
}
