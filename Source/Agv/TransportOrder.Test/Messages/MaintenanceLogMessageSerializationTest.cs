using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class MaintenanceLogMessageSerializationTest
{
    [TestMethod]
    public void GotMaintenanceLogs_ValidData_SerializesAllProperties()
    {
        var message = new GotMaintenanceLogs([new MaintenanceLog("user", Guid.NewGuid(), DateTime.UtcNow, MaintenanceType.TiresReplaced, "comment")]);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}