using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    internal interface IAssignAgvConsumerTestFixtureBuilder : ITestFixtureBuilder<AssignAgvConsumerMassTransitFixture>
    {
        [UsedImplicitly]
        IAssignAgvConsumerTestFixtureBuilder SetOrderRegistry(Mock<IOrderRegistry> orderRegistry);
        [UsedImplicitly]
        IAssignAgvConsumerTestFixtureBuilder ConfigureServices(Action<IServiceCollection> services);
        [UsedImplicitly]
        IAssignAgvConsumerTestFixtureBuilder OrderMock(Action<Mock<IOrderRegistry>> orderRegistry);
        IAssignAgvConsumerTestFixtureBuilder AgvRegistryMock(Action<Mock<IAgvRegistry>> agvRegistry);
        IAssignAgvConsumerTestFixtureBuilder SetupBusRegistrationConfigurator(Action<IBusRegistrationConfigurator> busRegistrationConfigurator);
    }
}