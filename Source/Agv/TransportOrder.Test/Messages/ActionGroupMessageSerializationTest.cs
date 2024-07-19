using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class ActionGroupMessageSerializationTest
{
    [TestMethod]
    public void QueueActionGroup_ValidData_SerializesAllProperties()
    {
        var message = new QueueActionGroup(AgvActionGenerator.GeneratePickDropActionGroup(), false);

        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}