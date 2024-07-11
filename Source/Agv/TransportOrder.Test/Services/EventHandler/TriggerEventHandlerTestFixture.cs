using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

public class TriggerEventHandlerTestFixture : MassTransitConsumerTestFixture<TriggerEventHandler>
{
    public TriggerEventHandlerTestFixture()
    {
        Build();
    }
}