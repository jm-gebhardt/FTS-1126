using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services.AgvPositions
{
    internal class AgvPositionReceivedConsumerMassTransitTestFixture : MassTransitTestFixture<AgvPositionReceivedConsumer>
    {
        public AgvPositionReceivedConsumerMassTransitTestFixture() : base(services =>
        {
            services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
            services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
            services.AddTransient(_ => new Mock<IAgvHubContext>().Object);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        })
        {

        }
    }
}
