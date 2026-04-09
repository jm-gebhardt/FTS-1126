using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Test.Helper;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class BackgroundMessageSerializationTest
{
    [TestMethod]
    public void AddOrUpdateBackground_ValidData_SerializesAllProperties()
    {
        var message = new AddOrUpdateBackground(BackgroundGenerator.Generate());
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}