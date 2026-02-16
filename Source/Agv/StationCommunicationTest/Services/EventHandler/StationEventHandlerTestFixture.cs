using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.StationCommunication.Services.EventHandler;

namespace Gebhardt.Acc.StationCommunication.Test.Services.EventHandler;

public class StationEventHandlerTestFixture : MassTransitConsumerTestFixture<StationEventHandler>
{
    public StationEventHandlerTestFixture()
    {
        Build();
    }
}
