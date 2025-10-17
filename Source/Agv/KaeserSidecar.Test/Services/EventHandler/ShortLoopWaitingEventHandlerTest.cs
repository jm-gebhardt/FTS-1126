using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class ShortLoopWaitingEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedShortLoopWaitingHandler_AklPickPsDrop_ActionHandled()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).StartedDate = DateTime.UtcNow;
        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();

        var agvPassedShortLoopWaitingHandler = new AgvPassedShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopWaitingHandler>();

        fixture.VerifyActionHandled(actionGroup.Actions.Single(a => a.Target.Node == null).CorrelationId, actionGroup.AssignedAgv!.Value, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopWaitingHandler_AklDropPsPick_ActionHandled()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).StartedDate = DateTime.UtcNow;
        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();

        var agvPassedShortLoopWaitingHandler = new AgvPassedShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopWaitingHandler>();

        fixture.VerifyActionHandled(actionGroup.Actions.Single(a => a.Target.Node == null).CorrelationId, actionGroup.AssignedAgv!.Value, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopWaitingHandler_ChargeActionGroup_ActionNotHandled()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionGroupState: ActionGroupState.Started, assignedAgv: Guid.NewGuid());
        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();

        var agvPassedShortLoopWaitingHandler = new AgvPassedShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopWaitingHandler>();

        fixture.VerifyNoActionHandled();
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopWaitingHandler_MoveActionGroup_ActionNotHandled()
    {
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        actionGroup.AssignedAgv = Guid.NewGuid();
        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();

        var agvPassedShortLoopWaitingHandler = new AgvPassedShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopWaitingHandler>();

        fixture.VerifyNoActionHandled();
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopWaitingHandler_PickAtApl_ActionNotHandled()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).StartedDate = DateTime.UtcNow;
        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();

        var agvPassedShortLoopWaitingHandler = new AgvPassedShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopWaitingHandler>();

        fixture.VerifyNoActionHandled();
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopWaitingHandler_DropAtApl_ActionNotHandled()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).StartedDate = DateTime.UtcNow;
        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();

        var agvPassedShortLoopWaitingHandler = new AgvPassedShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopWaitingHandler>();

        fixture.VerifyNoActionHandled();
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopWaitingHandler_AklDrop_DoppelspielAtPS_ActionNotHandled()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(actionGroupState: ActionGroupState.Started, source: "PS01", destination: "AP01", assignedAgv: Guid.NewGuid());
        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();

        var agvPassedShortLoopWaitingHandler = new AgvPassedShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopWaitingHandler>();

        fixture.VerifyNoActionHandled();
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopWaitingHandler_AklPickPsDrop_TriggerPassedDuringPick_ActionNotHandled()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).StartedDate = DateTime.UtcNow;
        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();

        var agvPassedShortLoopWaitingHandler = new AgvPassedShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopWaitingHandler>();

        fixture.VerifyNoActionHandled();
    }

    [TestMethod]
    public async Task Consume_AgvLeftShortLoopWaitingHandler_AklPickPsDrop_ActionHandlingStopped()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.Node = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.Last().ToGraphNode();
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).StartedDate = DateTime.UtcNow;

        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();
        var agvLeftShortLoopWaitingHandler = new AgvLeftShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvLeftShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvLeftShortLoopWaitingHandler>();

        fixture.VerifyActionHandingStopped(actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvLeftShortLoopWaitingHandler_AklDropPsPick_ActionHandlingStopped()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.Node = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.Last().ToGraphNode();
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).StartedDate = DateTime.UtcNow;

        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();
        var agvLeftShortLoopWaitingHandler = new AgvLeftShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvLeftShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvLeftShortLoopWaitingHandler>();

        fixture.VerifyActionHandingStopped(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvLeftShortLoopWaitingHandler_ChargeActionGroup_ActionHandlingNotStopped()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionGroupState: ActionGroupState.Started, assignedAgv: Guid.NewGuid());

        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();
        var agvLeftShortLoopWaitingHandler = new AgvLeftShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvLeftShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvLeftShortLoopWaitingHandler>();

        fixture.VerifyActionHandingNotStopped();
    }

    [TestMethod]
    public async Task Consume_AgvLeftShortLoopWaitingHandler_MoveActionGroup_ActionHandlingNotStopped()
    {
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        actionGroup.AssignedAgv = Guid.NewGuid();

        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();
        var agvLeftShortLoopWaitingHandler = new AgvLeftShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvLeftShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvLeftShortLoopWaitingHandler>();

        fixture.VerifyActionHandingNotStopped();
    }

    [TestMethod]
    public async Task Consume_AgvLeftShortLoopWaitingHandler_DropAtApl_ActionHandlingNotStopped()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).StartedDate = DateTime.UtcNow;

        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();
        var agvLeftShortLoopWaitingHandler = new AgvLeftShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvLeftShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvLeftShortLoopWaitingHandler>();

        fixture.VerifyActionHandingNotStopped();
    }

    [TestMethod]
    public async Task Consume_AgvLeftShortLoopWaitingHandler_PickAtApl_ActionHandlingNotStopped()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).StartedDate = DateTime.UtcNow;

        var fixture = new ShortLoopWaitingPointEventHandlerTestFixture();
        var agvLeftShortLoopWaitingHandler = new AgvLeftShortLoopWaitingHandler(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvLeftShortLoopWaitingHandler);

        await fixture.ShouldHaveConsumedAsync<AgvLeftShortLoopWaitingHandler>();

        fixture.VerifyActionHandingNotStopped();
    }
}