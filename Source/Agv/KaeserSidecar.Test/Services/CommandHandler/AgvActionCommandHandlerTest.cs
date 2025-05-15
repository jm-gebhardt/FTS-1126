using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.CommandHandler;

[TestClass]
public class AgvActionCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StartActionCommand_ValidData_PublishesFreeStation()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var startActionCommand = new StartActionCommand(actionGroup.AssignedAgv!.Value, "serial number", actionGroupMessageContent.Actions.First());

        await fixture.TestHarness.Bus.Publish(startActionCommand);
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldHavePublishedAsync<FreeStation>();
        fixture.VerifyActionDequeued(actionGroup.Actions.First().CorrelationId, Times.Never());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_ShortLoopPickAgvAlreadyAtStation_PublishesAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var startActionCommand = new StartActionCommand(actionGroup.AssignedAgv!.Value, "serial number", actionGroupMessageContent.Actions.First());

        await fixture.TestHarness.Bus.Publish(startActionCommand);
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_ShortLoopDropAgvAlreadyAtStation_DoesNotPublishPublishAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.Last().Nodes = [station, "other nodes"];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var startActionCommand = new StartActionCommand(actionGroup.AssignedAgv!.Value, "serial number", actionGroupMessageContent.Actions.Last());

        await fixture.TestHarness.Bus.Publish(startActionCommand);
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_AlreadyAtConveyorStation_NoShortLoopAction_PublishesAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.OrderBy(c => c.Value).Last().Key;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var startActionCommand = new StartActionCommand(actionGroup.AssignedAgv!.Value, "serial number", actionGroupMessageContent.Actions.First());

        await fixture.TestHarness.Bus.Publish(startActionCommand);
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_StartNodeNotConveyorStation_DoesNotPublishFreeStation()
    {
        const string station = "not conveyor station";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        var startActionCommand = new StartActionCommand(actionGroup.AssignedAgv!.Value, "serial number", actionGroupMessageContent.Actions.First());

        await fixture.TestHarness.Bus.Publish(startActionCommand);
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_NoNodes_DoesNotPublishFreeStation()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        var startActionCommand = new StartActionCommand(actionGroup.AssignedAgv!.Value, "serial number", actionGroupMessageContent.Actions.First());

        await fixture.TestHarness.Bus.Publish(startActionCommand);
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_UnknownActionCorrelationId_DoesNotPublishFreeStation()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(null);

        var startActionCommand = new StartActionCommand(actionGroup.AssignedAgv!.Value, "serial number", actionGroupMessageContent.Actions.First());

        await fixture.TestHarness.Bus.Publish(startActionCommand);
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_ValidData_AddAgvToStationOccupancy()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionCompleted = new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<DropActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_PickOnShortLoop_PublishesAgvFinishedShortLoopPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionCompleted = new PickActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<PickActionCompleted>();
        await fixture.ShouldHavePublishedAsync<AgvFinishedPick>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_UpdatedMove_PublishesAgvFinishedShortLoopPick()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var actionGroupMessageContent = order.CreateMovePickDropActionGroup(OrderGenerator.KaeserConfiguration, DateTime.UtcNow);
        actionGroupMessageContent.AssignedAgv = Guid.NewGuid();
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.ConvertFromMessage(actionGroupMessageContent);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        actionGroup.SetTargetForActionWithUpdate(pickAction.CorrelationId, station);
        actionGroup.SetTargetForAction(pickAction.CorrelationId, station);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionCompleted = new PickActionCompleted(actionGroup.CorrelationId, moveAction.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<PickActionCompleted>();
        await fixture.ShouldHavePublishedAsync<AgvFinishedPick>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_DropOnShortLoop_PublishesAgvFinishedDrop()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Destination];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        actionGroup.Actions.Last().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionCompleted = new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<DropActionCompleted>();
        await fixture.ShouldHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(order.Hu, orderDetails.Destination, station, Times.Once());
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_UnknownAction_StationOccupancyNotUpdated()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(null);

        var actionCompleted = new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, DateTime.UtcNow); 

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<DropActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_IsManual_DropAction_StationOccupancyNotUpdated()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station, completedDate: DateTime.UtcNow);
        actionGroup.IsManual = true;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(null);

        var actionCompleted = new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_NotManual_StationOccupancyNotUpdated()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station, completedDate: DateTime.UtcNow);
        actionGroup.IsManual = false;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(null);

        var actionCompleted = new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_ManualMove_PublishesOccupyStation()
    {
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(assignedAgv:Guid.NewGuid());
        actionGroup.CompleteAction(actionGroup.Actions.First().CorrelationId);
        actionGroup.IsManual = true;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        var actionCompleted = new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
    }
    
    [TestMethod]
    public async Task Consume_ActionAborted_ValidData_PublishesFreeStation()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var action = actionGroup.Actions.First();
        var actionGroupMessage = AgvActionGenerator.ConvertToMessage(actionGroup);
        var abortedAction = actionGroupMessage.Actions.First();
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        var actionAborted = new ActionAborted(action.CorrelationId, abortedAction);

        await fixture.TestHarness.Bus.Publish(actionAborted);
        await fixture.ShouldHaveConsumedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<FreeStation>();
    }
}