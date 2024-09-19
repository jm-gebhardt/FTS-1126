using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class AgvMessageSerializationTest
{
    [TestMethod]
    public void LockAgv_ValidaData_SerializesAllProperties()
    {
        var message = new LockAgv(Guid.NewGuid(), "reason", "InitiatingUser", AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, true);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void SetUserMode_ValidaData_SerializesAllProperties()
    {
        var message = new SetUserMode(Guid.NewGuid(), "InitiatingUser", AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, true);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void SetMaintenanceMode_ValidaData_SerializesAllProperties()
    {
        var message = new SetMaintenanceMode(Guid.NewGuid(), "reason", "InitiatingUser", AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, true);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void AgvModeChangePending_ValidData_SerializedAllProperties()
    {
        var agvMode = new AgvMode(ModeType.Normal, Guid.NewGuid());
        agvMode.PrepareSwitch(ModeType.Maintenance, "reason", "InitiatingUser", CurrentActionGroupPolicy.Pause, AssignedActionGroupPolicy.LeaveAssigned, true);
        var message = new AgvModeChangePending(agvMode.AgvCorrelationId, agvMode);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}