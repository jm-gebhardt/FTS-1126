using AutoMapper;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfMapRepositoryTestFixture : TransportOrderDbFixture
{
    private const string SmallImage1 = "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAIAQMAAAD+wSzIAAAABlBMVEX///+/v7+jQ3Y5AAAADklEQVQI12P4AIX8EAgALgAD/aNpbtEAAAAASUVORK5CYII=";
    public const string SmallImage2 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    public EfMapRepository Repository { get; }

    protected override IEnumerable<Profile> GetProfiles() => [new GraphProfile()];

    public EfMapRepositoryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfMapRepository>>();
        Repository = new EfMapRepository(ContextFactory, new SemaphoreProvider<IMapRepositorySemaphore>(new Mock<ILogger<SemaphoreProvider<IMapRepositorySemaphore>>>().Object), mockLogger.Object);
    }

    public Map GetNewMap(string mapName = "default")
    {
        const float resolution = 1;
        const float xOffset = 1;
        const float yOffset = 1;
        var modificationDate = DateTime.UtcNow;

        return new Map(mapName.ToGuid(), mapName, Convert.FromBase64String(SmallImage1), 8, 8, resolution, xOffset, yOffset, modificationDate);
    }
}