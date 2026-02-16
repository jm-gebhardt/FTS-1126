using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.OrderTranslator.Services.EventHandler;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.EventHandler;

public class StationEventHandlerTestFixture : MassTransitConsumerTestFixture<StationEventHandler>
{
    public StationEventHandlerTestFixture()
    {
        Build();
    }
}
