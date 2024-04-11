using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class GraphEventHandlerTestFixture : MassTransitConsumerTestFixture<GraphEventHandler>
{    
    public GraphEventHandlerTestFixture()
    {
        Build();
    }
}