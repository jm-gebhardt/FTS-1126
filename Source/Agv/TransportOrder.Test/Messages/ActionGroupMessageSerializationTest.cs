using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Guid = System.Guid;

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

    [TestMethod]
    public void StartActionOfActionGroup_ValidData_SerializesAllProperties()
    {
        var actionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutPickTarget(assignedAgv: Guid.NewGuid());
        var actionGroup = Helper.AgvActionGenerator.ConvertFromMessage(actionGroupMessage);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);
        actionGroup.StartAction(moveAction.CorrelationId, ["a node".ToGraphNode()], [Guid.NewGuid()]);
        actionGroup.SetTargetForActionWithUpdate(pickAction.CorrelationId, "target".ToGraphNode());
        actionGroup.CompleteAction(moveAction.CorrelationId);
        dropAction.Nodes = ["node 1".ToGraphNode(), "node2".ToGraphNode()];
        dropAction.Edges = [Guid.NewGuid()];
        dropAction.AdditionalInfo = "SAP:9070";
        var message = new StartActionOfActionGroup(dropAction, actionGroup.AssignedAgv!.Value);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}