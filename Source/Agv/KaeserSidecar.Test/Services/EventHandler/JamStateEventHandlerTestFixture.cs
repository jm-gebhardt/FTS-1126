using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

public class JamStateEventHandlerTestFixture: MassTransitConsumerTestFixture<JamStateEventHandler>
{
    public JamStateEventHandlerTestFixture()
    {
        Build();
    }
}