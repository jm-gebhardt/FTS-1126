using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Models;

[TestClass]
public class HostOrderTest
{
    [DataRow("1020", "2110")]
    [DataRow("1040", "2110")]
    [DataRow("1020", "2210")]
    [DataRow("1040", "2210")]
    [DataRow("1020", "2310")]
    [DataRow("1040", "2310")]
    [DataRow("1020", "2410")]
    [DataRow("1040", "2410")]
    [TestMethod]
    public void CreateActionGroup_AklDrop_MovePickDropActionGroup(string source, string target)
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateOrderDetails(source, target));

        var actionGroup = hostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration);
        
        VerifyActionGroup(actionGroup, hostOrder);
        actionGroup.Actions.Count.ShouldBe(3);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        moveAction.Sequence.ShouldBe(0);
        pickAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        pickAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId);
        dropAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        moveAction.Target.NodeId.ShouldNotBeNull();
        moveAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.RightShortLoopWaitingNode);
        pickAction.Target.NodeId.ShouldBeNullOrEmpty();
        dropAction.Target.NodeId.ShouldNotBeNull();
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[target]);
    }

    [DataRow("1020", "2110")]
    [DataRow("1040", "2110")]
    [DataRow("1020", "2210")]
    [DataRow("1040", "2210")]
    [DataRow("1020", "2310")]
    [DataRow("1040", "2310")]
    [DataRow("1020", "2410")]
    [DataRow("1040", "2410")]
    [TestMethod]
    public void CreateActionGroup_AklDrop_DoppelspielAtSource_PickDropActionGroup(string source, string target)
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateOrderDetails(source, target));
        var agv = Guid.NewGuid();
        const string pickNode = "PS03";
        
        var actionGroup = hostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration, agv, pickNode);
        
        VerifyActionGroup(actionGroup, hostOrder, agv);
        actionGroup.Actions.Count.ShouldBe(2);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        dropAction.Sequence.ShouldBe(1);

        pickAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId);
        dropAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        pickAction.Target.NodeId.ShouldBe(pickNode);
        dropAction.Target.NodeId.ShouldNotBeNull();
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[target]);
    }

    [DataRow("2130", "1020")]
    [DataRow("2130", "1050")]
    [DataRow("2230", "1020")]
    [DataRow("2230", "1050")]
    [DataRow("2330", "1020")]
    [DataRow("2330", "1050")]
    [DataRow("2430", "1020")]
    [DataRow("2430", "1050")]
    [TestMethod]
    public void CreateActionGroup_AklPick_PickMoveDropActionGroup(string source, string target)
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateOrderDetails(source, target));

        var actionGroup = hostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration);
        
        VerifyActionGroup(actionGroup, hostOrder);
        actionGroup.Actions.Count.ShouldBe(3);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        moveAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        pickAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId);
        dropAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        pickAction.Target.NodeId.ShouldNotBeNull();
        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[source]);
        moveAction.Target.NodeId.ShouldNotBeNull();
        moveAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.WaitingPositionByAklSource[source]);
        dropAction.Target.NodeId.ShouldBeNullOrEmpty();
    }

    [DataRow("2130", "1020")]
    [DataRow("2130", "1050")]
    [DataRow("2230", "1020")]
    [DataRow("2230", "1050")]
    [DataRow("2330", "1020")]
    [DataRow("2330", "1050")]
    [DataRow("2430", "1020")]
    [DataRow("2430", "1050")]
    [TestMethod]
    public void CreateActionGroup_AklPick_DoppelspielAtSource_PickMoveDropActionGroup(string source, string target)
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateOrderDetails(source, target));
        var agv = Guid.NewGuid();

        var actionGroup = hostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration, agv);

        VerifyActionGroup(actionGroup, hostOrder, agv);
        actionGroup.Actions.Count.ShouldBe(3);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        moveAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        pickAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId);
        dropAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        pickAction.Target.NodeId.ShouldNotBeNull();
        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[source]);
        moveAction.Target.NodeId.ShouldNotBeNull();
        moveAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.WaitingPositionByAklSource[source]);
        dropAction.Target.NodeId.ShouldBeNullOrEmpty();
    }

    [DataRow("9060", "1050")]
    [DataRow("9060", "1060")]
    [TestMethod]
    public void CreateActionGroup_AplPick_PickMoveDropActionGroup(string source, string target)
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateOrderDetails(source, target));

        var actionGroup = hostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration);

        VerifyActionGroup(actionGroup, hostOrder);
        actionGroup.Actions.Count.ShouldBe(3);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        moveAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        pickAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId);
        dropAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        pickAction.Target.NodeId.ShouldNotBeNull();
        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[source]);
        moveAction.Target.NodeId.ShouldNotBeNull();
        moveAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.LongLoopMoveTargetNode);
        dropAction.Target.NodeId.ShouldBeNullOrEmpty();
    }

    [DataRow("9060", "1050")]
    [DataRow("9060", "1060")]
    [TestMethod]
    public void CreateActionGroup_AplPick_DoppelspielAtSource_PickMoveDropActionGroup(string source, string target)
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateOrderDetails(source, target));
        var agv = Guid.NewGuid();
        string pickNode = OrderGenerator.KaeserConfiguration.StationMapping[source];
        var actionGroup = hostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration, agv, pickNode);

        VerifyActionGroup(actionGroup, hostOrder, agv);
        actionGroup.Actions.Count.ShouldBe(3);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        moveAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        pickAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId);
        dropAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        pickAction.Target.NodeId.ShouldNotBeNull();
        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[source]);
        moveAction.Target.NodeId.ShouldNotBeNull();
        moveAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.LongLoopMoveTargetNode);
        dropAction.Target.NodeId.ShouldBeNullOrEmpty();
    }

    [DataRow("1020", "9070")]
    [DataRow("1040", "9070")]
    [TestMethod]
    public void CreateActionGroup_AplDrop_MovePickDropActionGroup(string source, string target)
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateOrderDetails(source, target));

        var actionGroup = hostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration);

        VerifyActionGroup(actionGroup, hostOrder);
        actionGroup.Actions.Count.ShouldBe(3);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        moveAction.Sequence.ShouldBe(0);
        pickAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        pickAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId);
        dropAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        moveAction.Target.NodeId.ShouldNotBeNull();
        moveAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.LongLoopMoveTargetNode);
        pickAction.Target.NodeId.ShouldBeNullOrEmpty();
        dropAction.Target.NodeId.ShouldNotBeNull();
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[target]);
    }

    [DataRow("1020", "9070")]
    [DataRow("1040", "9070")]
    [TestMethod]
    public void CreateActionGroup_AplDrop_DoppelspielAtSource_PickDropActionGroup(string source, string target)
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateOrderDetails(source, target));
        var agv = Guid.NewGuid();
        const string pickNode = "PS03";

        var actionGroup = hostOrder.CreateActionGroup(OrderGenerator.KaeserConfiguration, agv, pickNode);

        VerifyActionGroup(actionGroup, hostOrder, agv);
        actionGroup.Actions.Count.ShouldBe(2);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        dropAction.Sequence.ShouldBe(1);

        pickAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId);
        dropAction.CorrelationId.ShouldBe(hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        pickAction.Target.NodeId.ShouldNotBeNull();
        pickAction.Target.NodeId.ShouldBe(pickNode);
        dropAction.Target.NodeId.ShouldNotBeNull();
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[target]);
    }

    private static void VerifyActionGroup(ActionGroup actionGroup, HostOrder hostOrder, Guid? assignedAgv=null)
    {
        actionGroup.CorrelationId.ShouldBe(hostOrder.CorrelationId);
        actionGroup.CreatedDate.ShouldBe(hostOrder.CreatedDate);
        actionGroup.Creator.ShouldBe(ActionGroupCreator.Wms);
        actionGroup.HandlingUnit.ShouldBe(hostOrder.Hu);
        actionGroup.AssignedAgv.ShouldBe(assignedAgv);
        actionGroup.Actions.All(a => a.ParentId == hostOrder.CorrelationId).ShouldBeTrue();
    }

    [TestMethod]
    public void Finish_ValidData_DatesSet()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        var finishTime = DateTime.UtcNow;
        hostOrder.Finish(finishTime);

        hostOrder.FinishedDate.ShouldBe(finishTime);
        hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).FinishedDate.ShouldBe(finishTime);
    }

    [TestMethod]
    public void FinishAction_ValidData_DateSet()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        var finishTime = DateTime.UtcNow;
        hostOrder.FinishAction(finishTime, ActionType.Pick);

        hostOrder.FailedDate.ShouldBeNull();
        hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).FinishedDate.ShouldBe(finishTime);
        hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).FinishedDate.ShouldBeNull();
    }

    [TestMethod]
    public void FinishAction_AlreadyFinished_DateNotUpdated()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        var finishTime = DateTime.UtcNow;
        hostOrder.FinishAction(finishTime, ActionType.Pick);
        hostOrder.FinishAction(DateTime.UtcNow, ActionType.Pick);
        hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).FinishedDate.ShouldBe(finishTime);
    }
}