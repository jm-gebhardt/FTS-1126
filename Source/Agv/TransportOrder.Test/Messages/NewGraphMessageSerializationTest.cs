using Gebhardt.Acc.Common.Messages.NewGraph;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Gebhardt.Acc.TransportOrder.Test.Helper;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class NewGraphMessageSerializationTest
{
    [TestMethod]
    public void NewGraphReceived_ValidData_SerializesAllProperties()
    {
        var message = new NewGraphSegmentReceived(Guid.NewGuid(), NewGraphGenerator.GenerateSegmentGraph());
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void NewGraphReceived_WithRoutingResources_SerializesAllProperties()
    {
        var message = new NewGraphSegmentReceived(Guid.NewGuid(), NewGraphGenerator.GenerateSegmentGraph(withRoutingResources: true));

        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void AddNewGraph_ValidData_SerializesAllProperties()
    {
        var message = new AddNewGraphSegment(Guid.NewGuid(), NewGraphGenerator.GenerateSegmentGraph());
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}