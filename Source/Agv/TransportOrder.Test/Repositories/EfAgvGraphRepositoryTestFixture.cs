using AutoMapper;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfAgvGraphRepositoryTestFixture : TransportOrderDbFixture
{
    private readonly SemaphoreProvider<IGraphRepositorySemaphore> _semaphoreProvider = new (new Mock<ILogger<SemaphoreProvider<IGraphRepositorySemaphore>>>().Object);

    public EfGraphRepository Repository { get; }

    protected override IEnumerable<Profile> GetProfiles() => [new GraphProfile()];

    public EfAgvGraphRepositoryTestFixture()
    {
        Repository = new EfGraphRepository(ContextFactory, Mapper, _semaphoreProvider, Mock.Of<ILogger<EfGraphRepository>>());
    }
}