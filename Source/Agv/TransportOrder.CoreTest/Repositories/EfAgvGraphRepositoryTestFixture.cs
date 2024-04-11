using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class EfAgvGraphRepositoryTestFixture : TransportOrderDbFixture
{
    private readonly SemaphoreProvider<IGraphRepositorySemaphore> _semaphoreProvider = new();

    public EfGraphRepository Repository { get; }

    protected override IEnumerable<Profile> GetProfiles() => [new GraphProfile()];

    public EfAgvGraphRepositoryTestFixture()
    {
        Repository = new EfGraphRepository(ContextFactory, Mapper, _semaphoreProvider, Mock.Of<ILogger<EfGraphRepository>>());
    }
}