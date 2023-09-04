using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TransportOrder.Domain.Models.Map;
using TransportOrder.Domain.SemaphoreProvider;
using TransportOrder.Persistence.Ef.DataAccess;
using TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;
public class EfMapRegistryTestFixture
{
    private const string SmallImage1 = "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAIAQMAAAD+wSzIAAAABlBMVEX///+/v7+jQ3Y5AAAADklEQVQI12P4AIX8EAgALgAD/aNpbtEAAAAASUVORK5CYII=";
    public const string SmallImage2 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

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
        const float resolution = 1;
        const float xOffset = 1;
        const float yOffset = 1;
        var modificationDate = DateTime.UtcNow;

        return new Map(mapId.ToGuid(), mapId, Convert.FromBase64String(SmallImage1), resolution, xOffset, yOffset, modificationDate);
    }
}