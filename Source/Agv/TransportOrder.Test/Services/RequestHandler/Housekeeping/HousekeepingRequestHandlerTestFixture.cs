using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Housekeeping;

public class HousekeepingRequestHandlerTestFixture<TRequestClient> : MediatorTestFixture<TRequestClient, HousekeepingRequestHandler, MockConsumer>
    where TRequestClient : class
{
    private readonly Mock<IHousekeepingConfigurationRepository> _housekeepingConfigurationRepository = new();

    public Mock<IHousekeepingConfigurationRepository> HousekeepingConfigurationRepository => _housekeepingConfigurationRepository;

    public HousekeepingRequestHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _housekeepingConfigurationRepository.Object);
        });
        base.Build();
    }
}
