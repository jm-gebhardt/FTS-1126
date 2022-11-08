using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.AgvPositions
{
    internal class AgvPositionReceivedConsumerMassTransitTestFixture : MassTransitTestFixture<AgvPositionReceivedConsumer>, ITestFixtureBuilder<AgvPositionReceivedConsumerMassTransitTestFixture>
    {
        private bool _useAgvPositionRegistryMock;
        public Mock<IAgvPositionRegistry>? AgvPositionRegistryMock { get; private set; }

        public AgvPositionReceivedConsumerMassTransitTestFixture()
        {
            Build(ConfigureServiceCollection);
        }

        public ITestFixtureBuilder<AgvPositionReceivedConsumerMassTransitTestFixture> UseAgvPositionRegistryMock()
        {
            _useAgvPositionRegistryMock = true;
            return this;
        }

        public AgvPositionReceivedConsumerMassTransitTestFixture Build()
        {
            Build(ConfigureServiceCollection);
            return this;
        }

        private void ConfigureServiceCollection(IServiceCollection services)
        {
            if (_useAgvPositionRegistryMock)
            {
                AgvPositionRegistryMock = new Mock<IAgvPositionRegistry>();
                services.AddSingleton(AgvPositionRegistryMock.Object);
            }
            else
            {
                services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
            }

            services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
            services.AddTransient(_ => new Mock<IAgvHubContext>().Object);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        }

    }
}
