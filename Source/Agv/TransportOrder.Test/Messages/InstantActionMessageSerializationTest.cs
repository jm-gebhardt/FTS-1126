using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using StartInstantAction = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.StartInstantAction;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class InstantActionMessageSerializationTest
{
    [TestMethod]
    public void StartInstantAction_NoParameters_SerializesAllProperties()
    {
        var message = new StartInstantAction(Guid.NewGuid(), "action Type", null, null, "user");

        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void StartInstantAction_BlockingType_SerializesAllProperties()
    {
        var message = new StartInstantAction(Guid.NewGuid(), "action Type", null, BlockingType.HARD, "user");

        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void StartInstantAction_WithParameters_SerializesAllProperties()
    {
        var message = new StartInstantAction(Guid.NewGuid(), "action Type", new Dictionary<string, object>()
        {
            {"string", "string"},
            {"integer", 42},
            {"float", 42.42},
            {"bool", true}
        }, null, "user");
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}