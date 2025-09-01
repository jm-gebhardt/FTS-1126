using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class TriggerMessageSerializationTest
{
    [TestMethod]
    public void AssignTriggerToLocation_ValidaData_SerializesAllProperties()
    {
        var message = new AssignTriggerToLocation(Guid.NewGuid(), "nodeId", Guid.NewGuid(), [new PolygonPoint(12, 12)], Guid.NewGuid(), [new ConfigurationParameter("name", "description", "value", "validation")]);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void TriggerLocationAssigned_ValidData_SerializesAllProperties()
    {
        var message = new TriggerLocationAssigned(new Location("nodeId", Guid.NewGuid(), Guid.NewGuid(), [new ConfigurationParameter("name", "description", "value", "validation")]));

        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void AddTriggerLocation_ValidData_SerializesAllProperties()
    {
        var message = new AddTriggerLocation(Guid.NewGuid(), "nodeId", Guid.NewGuid(), [new PolygonPoint(12, 12)], Guid.NewGuid(), [new ConfigurationParameter("name", "description", "value", "validation")]);

        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}