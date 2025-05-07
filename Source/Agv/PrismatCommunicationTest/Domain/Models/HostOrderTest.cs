using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Models;

[TestClass]
public class HostOrderTest
{
    [TestMethod]
    public void TryCreatePickDropActionGroup_ShortCircuitOrder_PickDropActionGroup()
    {
        var shortCircuitOrder = OrderGenerator.GenerateShortCircuitOrder();
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(shortCircuitOrder);

        var actionGroup = hostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, shortCircuitOrder.CreationDate);
        actionGroup.ShouldNotBeNull();
        actionGroup.Actions.Count.ShouldBe(2);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        dropAction.Sequence.ShouldBe(1);

        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[shortCircuitOrder.Source]);
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[shortCircuitOrder.Destination]); 
    }

    [DataRow(0)] //1020
    [DataRow(1)] //1040
    [DataTestMethod]
    public void TryCreatePickDropActionGroup_LongCircuitOrderWithIncompleteTarget_null(int input)
    {
        var longCircuitOrder = OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: input);
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(longCircuitOrder);

        var actionGroup = hostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, longCircuitOrder.CreationDate);
        actionGroup.ShouldBeNull();
    }

    [DataRow(0)] //1020
    [DataRow(1)] //1040
    [DataTestMethod]
    public void CreateMovePickDropActionGroup_ValidData_MovePickDropActionGroup(int input)
    {
        var longCircuitOrder = OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: input);
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(longCircuitOrder);
        var actionGroup = hostOrder.CreateMovePickDropActionGroup(OrderGenerator.KaeserConfiguration, longCircuitOrder.CreationDate);

        actionGroup.Actions.Count.ShouldBe(3);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        moveAction.Sequence.ShouldBe(0);
        pickAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        moveAction.Target.NodeId.ShouldBe(OrderGenerator.DecisionNode);
        pickAction.Target.NodeId.ShouldBeEmpty();
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[longCircuitOrder.Destination]);
    }

    [DataRow(0)] //1050
    [DataRow(1)] //1060
    [DataTestMethod]
    public void CreatePickMoveDropActionGroup_ValidData_PickMoveDropActionGroup(int output)
    {
        var longCircuitOrder = OrderGenerator.GenerateUnknownDropTargetOrder(dropTarget: output);
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(longCircuitOrder);
        var actionGroup = hostOrder.CreatePickMoveDropActionGroup(OrderGenerator.KaeserConfiguration);

        actionGroup.Actions.Count.ShouldBe(3);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        pickAction.Sequence.ShouldBe(0);
        moveAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[longCircuitOrder.Source]);
        moveAction.Target.NodeId.ShouldBe(OrderGenerator.DecisionNode);
        dropAction.Target.NodeId.ShouldBeEmpty();
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