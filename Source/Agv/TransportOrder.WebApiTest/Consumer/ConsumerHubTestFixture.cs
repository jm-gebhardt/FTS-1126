using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer
{
    public class ConsumerHubTestFixture<TConsumer, THubContext> : MassTransitTestFixture<TConsumer>
        where TConsumer : class, MassTransit.IConsumer
        where THubContext : class
    {
        public readonly Mock<THubContext> MockHub = new();
        public readonly Mock<INodeStateRegistry> MockNodeStateRegistry = new();
        
        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddTransient(_ => MockHub.Object);
            collection.AddTransient(_ => MockNodeStateRegistry.Object);
            collection.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        }
    }
}
