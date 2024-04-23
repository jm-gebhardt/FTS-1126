using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

public class AgvDataEventHandlerTestFixture : MassTransitConsumerTestFixture<AgvDataEventHandler>
{
    public AgvDataEventHandlerTestFixture()
    {
        Build();
    }
}