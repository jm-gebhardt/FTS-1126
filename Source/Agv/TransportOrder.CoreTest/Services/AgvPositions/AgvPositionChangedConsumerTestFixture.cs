using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.AgvPositions
{
    public class AgvPositionChangedConsumerTestFixture: MassTransitTestFixture<AgvPositionChangedConsumer>
    {
        public readonly Mock<IAgvHubContext> MockHub = new();
        public readonly Mock<INodeStateRegistry> MockNodeStateRegistry = new();
        
        
        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddTransient(_ => MockHub.Object);
            collection.AddTransient(_ => MockNodeStateRegistry.Object);
            collection.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        }
    }
}


