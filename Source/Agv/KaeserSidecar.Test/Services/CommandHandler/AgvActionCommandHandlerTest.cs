using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
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
    public async Task Consume_ActionStarted_ValidData_PublishesFreeStation()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_ActionStarted_ShortLoopPickAgvAlreadyAtStation_PublishesAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];
      
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);
        
        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_ActionStarted_ShortLoopDropAgvAlreadyAtStation_DoesNotPublishPublishAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.Last().Nodes = [station, "other nodes"];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionStarted = new ActionStarted(actionGroup.Actions.Last().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
    }

    [TestMethod]
    public async Task Consume_ActionStarted_AlreadyAtConveyorStation_NoShortLoopAction_PublishesAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.OrderBy(c => c.Value).Last().Key;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_ActionStarted_StartNodeNotConveyorStation_DoesNotPublishFreeStation()
    {
        const string station = "not conveyor station";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_ActionStarted_NoNodes_DoesNotPublishFreeStation()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [];
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_ActionStarted_UnknownActionCorrelationId_DoesNotPublishFreeStation()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        var actionStarted = new ActionStarted("Unknown".ToGuid(), actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStation>();
    }

    [TestMethod]
    public async Task Consume_ActionCompleted_ValidData_AddAgvToStationOccupancy()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionCompleted = new ActionCompleted(actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_ActionCompleted_PickOnShortLoop_PublishesAgvFinishedShortLoopPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionCompleted = new ActionCompleted(actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldHavePublishedAsync<AgvFinishedPick>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_ActionCompleted_DropOnShortLoop_PublishesAgvFinishedDrop()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Destination];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        actionGroup.Actions.Last().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionCompleted = new ActionCompleted(actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(order.Hu, orderDetails.Destination, station, Times.Once());
    }

    [TestMethod]
    public async Task Consume_ActionCompleted_UnknownAction_StationOccupancyNotUpdated()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        var actionCompleted = new ActionCompleted("Unknown".ToGuid());

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedPick>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
    }
}