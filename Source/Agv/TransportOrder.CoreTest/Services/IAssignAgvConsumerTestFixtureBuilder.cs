using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    internal interface IAssignAgvConsumerTestFixtureBuilder : IMassTransitTestFixtureBuilder<AssignAgvConsumerMassTransitFixture, IAssignAgvConsumerTestFixtureBuilder>
    {
        [UsedImplicitly]
        IAssignAgvConsumerTestFixtureBuilder SetOrderRegistry(Mock<IOrderRegistry> orderRegistry);
        [UsedImplicitly]
        IAssignAgvConsumerTestFixtureBuilder ConfigureServices(Action<IServiceCollection> services);
        [UsedImplicitly]
        IAssignAgvConsumerTestFixtureBuilder OrderMock(Action<Mock<IOrderRegistry>> orderRegistry);
        IAssignAgvConsumerTestFixtureBuilder AgvRegistryMock(Action<Mock<IAgvRegistry>> agvRegistry);
    }
}