using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Maps
{
    public class EfMapRegistryTestFixture
    {
        public EfMapRegistry Registry { get; }

        private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }
        private ServiceProvider Provider { get; }

        public EfMapRegistryTestFixture()
        {
            var mockLogger = new Mock<ILogger<EfMapRegistry>>();
            var collection = new ServiceCollection()
                .AddDbContextFactory<TransportOrderDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    }
                );
            Provider = collection.BuildServiceProvider(true);
            ContextFactory = Provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
            Registry = new EfMapRegistry(ContextFactory, mockLogger.Object, new MapRegistrySemaphoreProvider());
        }

        public void Add(Map map)
        {
            using var context = ContextFactory.CreateDbContext();
            context.Set<Map>().Add(map);
            context.SaveChanges();
        }

        public Map GetNewMap(string mapId)
        {
            const string imageData = "aW1hZ2VJbWFnZUlNQUdF";
            const float resolution = 1;
            const float xOffset = 1;
            const float yOffset = 1;
            var modificationDate = DateTime.UtcNow;

            return new Map(mapId.ToGuid(), mapId, Convert.FromBase64String(imageData), resolution, xOffset, yOffset, modificationDate);
        }
    }
}