using AutoMapper;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfMapRepositoryTestFixture : TransportOrderDbFixture
{
    public const string SmallImage2 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    public EfMapRepository Repository { get; }

    protected override IEnumerable<Profile> GetProfiles() => [new GraphProfile()];

    public EfMapRepositoryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfMapRepository>>();
        Repository = new EfMapRepository(ContextFactory, new SemaphoreProvider<IMapRepositorySemaphore>(new Mock<ILogger<SemaphoreProvider<IMapRepositorySemaphore>>>().Object), mockLogger.Object);
    }
}