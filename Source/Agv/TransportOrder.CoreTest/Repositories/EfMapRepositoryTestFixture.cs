using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class EfMapRepositoryTestFixture : TransportOrderDbFixture
{
    private const string SmallImage1 = "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAIAQMAAAD+wSzIAAAABlBMVEX///+/v7+jQ3Y5AAAADklEQVQI12P4AIX8EAgALgAD/aNpbtEAAAAASUVORK5CYII=";
    public const string SmallImage2 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    public EfMapRepository Repository { get; }

    protected override IEnumerable<Profile> GetProfiles() => [new GraphProfile()];

    public EfMapRepositoryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfMapRepository>>();
        Repository = new EfMapRepository(ContextFactory, Mapper, new SemaphoreProvider<IMapRepositorySemaphore>(), mockLogger.Object);
    }

    public Map GetNewMap(string mapId = "default")
    {
        const float resolution = 1;
        const float xOffset = 1;
        const float yOffset = 1;
        var modificationDate = DateTime.UtcNow;

        return new Map(mapId.ToGuid(), mapId, Convert.FromBase64String(SmallImage1), 8, 8, resolution, xOffset, yOffset, modificationDate);
    }
}