using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer
{
    internal class AgvPositionReceivedConsumerMassTransitTestFixture : MassTransitTestFixture<AgvPositionReceivedConsumer>, ITestFixtureBuilder<AgvPositionReceivedConsumerMassTransitTestFixture>
    {
        private bool _useAgvPositionRegistryMock;
        public Mock<IAgvPositionRegistry>? AgvPositionRegistryMock { get; private set; }
        private Mock<IGraphRegistry>? GraphRegistryMock { get; set; }

        public AgvPositionReceivedConsumerMassTransitTestFixture()
        {
            base.Build();
        }

        public ITestFixtureBuilder<AgvPositionReceivedConsumerMassTransitTestFixture> UseAgvPositionRegistryMock()
        {
            _useAgvPositionRegistryMock = true;
            return this;
        }

        public AgvPositionReceivedConsumerMassTransitTestFixture Build()
        {
            base.Build();
            return this;
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            GraphRegistryMock = new Mock<IGraphRegistry>();

            if (_useAgvPositionRegistryMock)
            {
                AgvPositionRegistryMock = new Mock<IAgvPositionRegistry>();

                services.AddSingleton(AgvPositionRegistryMock.Object);
                services.AddSingleton(GraphRegistryMock.Object);
            }
            else
            {
                services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
                services.AddSingleton(GraphRegistryMock.Object);
            }

            services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
            services.AddTransient(_ => new Mock<IAgvHubContext>().Object);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        }
    }
}
