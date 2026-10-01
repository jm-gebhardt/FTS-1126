using Gebhardt.Acc.Common.Messages.NewGraph;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Test.Helper;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

internal class NewGraphEventHandlerTestFixture : MassTransitConsumerTestFixture<NewGraphEventHandler>
{
    public NewGraphSegmentReceived NewGraphSegmentReceived { get; } = new(Guid.NewGuid(), NewGraphGenerator.GenerateSegmentGraph());

    public NewGraphEventHandlerTestFixture()
    {
        Build();
    }
}