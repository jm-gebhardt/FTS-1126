using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Events.RestrictedArea;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class RestrictedAreaSerializationTest
{
    [TestMethod]
    public void UpdateRestrictedArea_ValidData_SerializesAllProperties()
    {
        var restrictedArea = new RestrictedArea { Id = Guid.NewGuid(), Area = [new RestrictedAreaPolygonPoint(1, 1, 0, Guid.NewGuid())] };
        var message = new RestrictedAreaUpdated(restrictedArea);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}