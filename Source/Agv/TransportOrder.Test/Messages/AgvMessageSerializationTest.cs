using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class AgvMessageSerializationTest
{
    [TestMethod]
    public void LockAgv_ValidaData_SerializesAllProperties()
    {
        var message = new LockAgv(Guid.NewGuid(), LockReason.Other, "description", "InitiatingUser", AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, true);
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
        var message = new SetMaintenanceMode(Guid.NewGuid(), "InitiatingUser", MaintenanceReason.Other, "description",  AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, true);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void AgvModeChangePending_ValidData_SerializedAllProperties()
    {
        var agvMode = new AgvMode(ModeType.Normal, Guid.NewGuid());
        
        agvMode.PrepareSwitch(
            ModeType.Maintenance, 
            lockReason:LockReason.Other,
            reasonDescription: "reason description",
            initiatingUser:"InitiatingUser", 
            currentActionPolicy: CurrentActionGroupPolicy.Pause, 
            assignedActionPolicy: AssignedActionGroupPolicy.LeaveAssigned, 
            cancelManualActions: true);
        
        var message = new AgvModeChangePending(agvMode.AgvCorrelationId, agvMode);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}