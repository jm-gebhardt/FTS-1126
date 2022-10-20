using Microsoft.Extensions.DependencyInjection;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;

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
