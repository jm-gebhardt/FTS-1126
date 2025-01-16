using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.CommandHandler;

[TestClass]
public class AgvActionCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionStarted_ValidData_RemoveAgvFromStationOccupancy()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        fixture.Repository.OccupyStation(station, actionGroup.AssignedAgv!.Value);
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(1);
        
        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        fixture.Repository.GetStationOccupancy().Select(o => o.Count).Sum().ShouldBe(0);
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

        fixture.Repository.OccupyStation(station, actionGroup.AssignedAgv!.Value);
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(1);

        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldHavePublishedAsync<AgvStartedPick>();
        fixture.Repository.GetStationOccupancy().Select(o => o.Count).Sum().ShouldBe(0);
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
        actionGroup.Actions.First().Nodes = [station, "other nodes"];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldHavePublishedAsync<AgvStartedPick>();
    }

    [TestMethod]
    public async Task Consume_ActionStarted_StartNodeNotConveyorStation_AgvNotRemovedFromStationOccupancy()
    {
        const string station = "not conveyor station";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);
        fixture.Repository.OccupyStation(station, actionGroup.AssignedAgv!.Value);
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(1);

        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        fixture.Repository.GetStationOccupancy().Select(o => o.Count).Sum().ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_ActionStarted_NoNodes_AgvNotRemovedFromStationOccupancy()
    {
        const string station = "not conveyor station";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [];
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);
        fixture.Repository.OccupyStation(station, actionGroup.AssignedAgv!.Value);
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(1);

        var actionStarted = new ActionStarted(actionGroup.Actions.First().CorrelationId, actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        fixture.Repository.GetStationOccupancy().Select(o => o.Count).Sum().ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_ActionStarted_UnknownActionCorrelationId_AgvNotRemovedFromStationOccupancy()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);
        fixture.Repository.OccupyStation(station, actionGroup.AssignedAgv!.Value);
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(1);

        var actionStarted = new ActionStarted("Unknown".ToGuid(), actionGroup.AssignedAgv!.Value);

        await fixture.TestHarness.Bus.Publish(actionStarted);
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_ActionCompleted_ValidData_AddAgvToStationOccupancy()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(0);

        var actionCompleted = new ActionCompleted(actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedPick>();
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(1);
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
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(0);

        var actionCompleted = new ActionCompleted(actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldHavePublishedAsync<AgvFinishedPick>();
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_ActionCompleted_TargetNodeNotConveyorStation_StationOccupancyNotUpdated()
    {
        const string station = "not conveyor station";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);
        fixture.Repository.GetStationOccupancy().Select(o => o.Count).Sum().ShouldBe(0);

        var actionCompleted = new ActionCompleted(actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId);

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedPick>();
        fixture.Repository.GetStationOccupancy().Select(o => o.Count).Sum().ShouldBe(0);
    }

    [TestMethod]
    public async Task Consume_ActionCompleted_UnknownAction_StationOccupancyNotUpdated()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(0);

        var actionCompleted = new ActionCompleted("Unknown".ToGuid());

        await fixture.TestHarness.Bus.Publish(actionCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedPick>();
        fixture.Repository.GetStationOccupancy().Single(o => o.Station == station).Count.ShouldBe(0);
    }
}