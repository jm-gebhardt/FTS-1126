using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Moq;
using Shouldly;
using AgvStatusGenerator = Gebhardt.Acc.Common.Testing.Helpers.AgvStatusGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

[TestClass]
public class DoppelspielCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionGroupCreated_ActionGroupsAlreadyQueued_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var actionGroupCreated = new ActionGroupCreated(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, null);

        await fixture.PublishMessageAsync(actionGroupCreated);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_MoveActionGroup_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var actionGroupCreated = new ActionGroupCreated(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, null);

        await fixture.PublishMessageAsync(actionGroupCreated);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_NoDoppelspielDrop_PublishesQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithDropAction(null)
            .WithStationGroupLimit(true)
            .AddActionGroup(actionGroup);

        var actionGroupCreated = new ActionGroupCreated(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, null);

        await fixture.PublishMessageAsync(actionGroupCreated);

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_AgvStationLimitationViolated_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithDropAction(null)
            .WithStationGroupLimit(false)
            .AddActionGroup(actionGroup);

        var actionGroupCreated = new ActionGroupCreated(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, null);

        await fixture.PublishMessageAsync(actionGroupCreated);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_DoppelspielDrop_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false);
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        var dropActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: true, assignedAgv: Guid.NewGuid(), canRunDoppelspiel: true);
        var doppelspielDropAction = dropActionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithDropAction(dropActionGroup)
            .WithStationGroupLimit(true)
            .AddActionGroup(actionGroup);

        var actionGroupCreated = new ActionGroupCreated(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, null);

        await fixture.PublishMessageAsync(actionGroupCreated);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        doppelspielDropAction.IsRegisteredForDoppelspiel.ShouldBeTrue();
        pickAction.IsRegisteredForDoppelspiel.ShouldBeTrue();
        actionGroup.AssignedAgv.ShouldBe(dropActionGroup.AssignedAgv);
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_ActionGroupNotConsideredForDoppelspiel_DropOrderDoppelspielModeOrderOrQueueHead_EmptyQueue_CanRunDoppelspiel()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false, canRunDoppelspiel: false);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.OrderOrQueueHead;
        var queue = QueueGenerator.GenerateEmptyQueue();
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStationGroupLimit(true)
            .AddActionGroup(actionGroup)
            .WithRoute([Guid.NewGuid(), Guid.NewGuid()])
            .WithQueuesOnRoute([queue])
            .AddAgv(AgvGenerator.GenerateIdleAgv());

        var actionGroupCreated = new ActionGroupCreated(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, null);

        await fixture.PublishMessageAsync(actionGroupCreated);
        actionGroup.CanRunDoppelspiel.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCreated_PickAlwaysRunWithDoppelspiel_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false);
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.Target.DoppelspielMode = DoppelspielMode.Always;
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var actionGroupCreated = new ActionGroupCreated(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, null);

        await fixture.PublishMessageAsync(actionGroupCreated);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvLeftStation_DelayedOrderForStationGroup_NotLimited_PublishesQueueActionGroup()
    {
        var station = StationGenerator.GetPickDropStation("AP01", stationGroup: ["AP02".ToGuid()]);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false, destination: station.Name);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextDelayedDropForStations(actionGroup)
            .WithStationGroupLimit(true);

        await fixture.PublishMessageAsync(new AgvLeftStation(station.Id, Guid.NewGuid()));

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvLeftStation_DelayedOrderForStationGroup_StillAgvStationLimited_DoesNotPublishQueueActionGroup()
    {
        var station = StationGenerator.GetPickDropStation("AP01", stationGroup: ["AP02".ToGuid()]);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false, destination: station.Name);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextDelayedDropForStations(actionGroup)
            .WithStationGroupLimit(false);

        await fixture.PublishMessageAsync(new AgvLeftStation(station.Id, Guid.NewGuid()));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvLeftStation_NoDelayedOrderForStationGroup_DoesNotPublishQueueActionGroup()
    {
        var station = StationGenerator.GetPickDropStation("AP01", stationGroup: ["AP02".ToGuid()]);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextDelayedDropForStations(null);

        await fixture.PublishMessageAsync(new AgvLeftStation(station.Id, Guid.NewGuid()));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvLeftStation_SingleStation_DoesNotPublishQueueActionGroup()
    {
        var station = StationGenerator.GetPickDropStation("APL1", stationGroup: []);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new AgvLeftStation(station.Id, Guid.NewGuid()));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_NoActionGroupInMessage_DoesNotPublish()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var fixture = new DoppelspielCommandHandlerTestFixture();

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, null, [], [], "StationName");

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_NoPickDropActionGroup_DoesNotPublish()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], "StationName");

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task TriggerDoppelspielPartnerQueueing_HasAlreadyQueuedDoppelspielPartner_RespondsDoppelspielPartnerQueuedWithNull()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.RegisterDoppelspiel(ActionType.Drop);
        actionGroup.SetDoppelspielpartner(Guid.NewGuid(), ActionType.Drop);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var doppelspielPartnerQueued = await fixture.PublishAndGetFirstMessageAsync<TriggerDoppelspielPartnerQueueing, DoppelspielPartnerQueued>(new TriggerDoppelspielPartnerQueueing(actionGroup.CorrelationId, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.Node!));

        doppelspielPartnerQueued.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task TriggerDoppelspielPartnerQueueing_NoDelayedOrder_RespondsDoppelspielPartnerQueuedWithNull()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.RegisterDoppelspiel(ActionType.Drop);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithNextPickForStation(null)
            .AddActionGroup(actionGroup);

        var doppelspielPartnerQueued = await fixture.PublishAndGetFirstMessageAsync<TriggerDoppelspielPartnerQueueing, DoppelspielPartnerQueued>(new TriggerDoppelspielPartnerQueueing(actionGroup.CorrelationId, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.Node!));

        doppelspielPartnerQueued.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [DataRow(DoppelspielMode.Always)]
    [DataRow(DoppelspielMode.None)]
    [TestMethod]
    public async Task TriggerDoppelspielPartnerQueueing_NextPickHasWrongDoppelspielMode_RespondsDoppelspielPartnerQueuedWithNull(DoppelspielMode mode)
    {
        const string station = "AP01";
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        actionGroup.RegisterDoppelspiel(ActionType.Drop);
        var nextPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(source: station);
        var pick = nextPickActionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pick.Target.DoppelspielMode = mode;
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithNextPickForStation(nextPickActionGroup)
            .AddActionGroup(actionGroup);

        var doppelspielPartnerQueued = await fixture.PublishAndGetFirstMessageAsync<TriggerDoppelspielPartnerQueueing, DoppelspielPartnerQueued>(new TriggerDoppelspielPartnerQueueing(actionGroup.CorrelationId, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.Node!));

        doppelspielPartnerQueued.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task TriggerDoppelspielPartnerQueueing_AgvHasAssignedActionGroup_RespondsDoppelspielPartnerQueuedWithNull()
    {
        const string station = "AP01";
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        actionGroup.RegisterDoppelspiel(ActionType.Drop);
        var nextPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(source: station);
        var pick = nextPickActionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pick.Target.DoppelspielMode = DoppelspielMode.Order;
        var assignedActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: actionGroup.AssignedAgv, source: station);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithNextPickForStation(nextPickActionGroup)
            .WithCreatedAndStartedActionGroups([assignedActionGroup])
            .AddActionGroup(actionGroup);

        var doppelspielPartnerQueued = await fixture.PublishAndGetFirstMessageAsync<TriggerDoppelspielPartnerQueueing, DoppelspielPartnerQueued>(new TriggerDoppelspielPartnerQueueing(actionGroup.CorrelationId, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.Node!));

        doppelspielPartnerQueued.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task TriggerDoppelspielPartnerQueueing_AgvStationLimitViolated_RespondsDoppelspielPartnerQueuedWithNull()
    {
        const string station = "AP01";
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        actionGroup.RegisterDoppelspiel(ActionType.Drop);
        var nextPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(source: station);
        var pick = nextPickActionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pick.Target.DoppelspielMode = DoppelspielMode.Order;
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithNextPickForStation(nextPickActionGroup)
            .WithCreatedAndStartedActionGroups([])
            .WithStationGroupLimit(false)
            .AddActionGroup(actionGroup);

        var doppelspielPartnerQueued = await fixture.PublishAndGetFirstMessageAsync<TriggerDoppelspielPartnerQueueing, DoppelspielPartnerQueued>(new TriggerDoppelspielPartnerQueueing(actionGroup.CorrelationId, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.Node!));

        doppelspielPartnerQueued.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task TriggerDoppelspielPartnerQueueing_ValidData_RespondsDoppelspielPartnerQueuedWithCorrelationId()
    {
        const string station = "AP01";
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        actionGroup.RegisterDoppelspiel(ActionType.Drop);
        var nextPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(source: station);
        var pick = nextPickActionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pick.Target.DoppelspielMode = DoppelspielMode.Order;
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithNextPickForStation(nextPickActionGroup)
            .WithCreatedAndStartedActionGroups([])
            .WithStationGroupLimit(true)
            .AddActionGroup(actionGroup);

        var doppelspielPartnerQueued = await fixture.PublishAndGetFirstMessageAsync<TriggerDoppelspielPartnerQueueing, DoppelspielPartnerQueued>(new TriggerDoppelspielPartnerQueueing(actionGroup.CorrelationId, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.Node!));

        doppelspielPartnerQueued.CorrelationId.ShouldBe(nextPickActionGroup.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [DataRow(ActionState.Failed)]
    [DataRow(ActionState.Created)]
    [DataRow(ActionState.Canceled)]
    [DataRow(ActionState.Aborted)]
    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_ActionInWrongState_DoesNotPublish(ActionState actionState)
    {
        const string station = "PS01";
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: actionState, destination: station);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_ActionHasDifferentTarget_DoesNotPublish()
    {
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: ActionState.Completed, destination: "PS01");
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], "PS02");

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_ManualOrderWithoutSap_DoesNotPublishManualActionPassedDoppelspielTrigger()
    {
        const string station = "PS01";
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            isManual: true,
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: ActionState.Completed, destination: station);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_ManualOrder_PublishesManualActionPassedDoppelspielTrigger()
    {
        const string station = "PS01";
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            isManual: true,
            pickActionState: ActionState.Completed, source: station,
            dropActionState: ActionState.Completed, destination: "APL1");
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.AdditionalInfo = "SAP:1020";
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_Pick_PublishesPickingAgvPassedDoppelspielTriggerPoint()
    {
        const string station = "PS01";
        var agvData = AgvDataGenerator.GenerateAgvData(Guid.NewGuid());
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: station,
            dropActionState: ActionState.Completed, destination: "APL1");
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DropNoDoppelspielConfiguredForTarget_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var agv = AgvGenerator.GenerateBusyAgv();
        var agvData = AgvDataGenerator.GenerateAgvData(agv.CorrelationId);
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: ActionState.Completed, destination: station.Name, canRunDoppelspiel: true);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.None;
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .AddActionGroup(actionGroup)
            .AddAgv(agv);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = await fixture.GetFirstPublishedMessageAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        droppingAgvPassedDoppelspielTriggerPoint.NextPickHostOrderCorrelationId.ShouldBeNull();
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(dropAction.Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(actionGroup.AssignedAgv.GetValueOrDefault());
        droppingAgvPassedDoppelspielTriggerPoint.DroppingHostOrderCorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    public async Task Consume_AgvPassedDoppelspielTriggerPoint_OrderCanNotRunDoppelspiel_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var agv = AgvGenerator.GenerateBusyAgv();
        var agvData = AgvDataGenerator.GenerateAgvData(agv.CorrelationId);
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: ActionState.Completed, destination: station.Name, canRunDoppelspiel: false);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.Order;
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .AddActionGroup(actionGroup)
            .AddAgv(agv);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = await fixture.GetFirstPublishedMessageAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        droppingAgvPassedDoppelspielTriggerPoint.NextPickHostOrderCorrelationId.ShouldBeNull();
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(dropAction.Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(actionGroup.AssignedAgv.GetValueOrDefault());
        droppingAgvPassedDoppelspielTriggerPoint.DroppingHostOrderCorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DropNoNextPick_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var agv = AgvGenerator.GenerateBusyAgv();
        var agvData = AgvDataGenerator.GenerateAgvData(agv.CorrelationId);
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: ActionState.Completed, destination: station.Name, canRunDoppelspiel: true);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.Order;
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextPickForStation(null)
            .AddActionGroup(actionGroup)
            .AddAgv(agv);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = await fixture.GetFirstPublishedMessageAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        droppingAgvPassedDoppelspielTriggerPoint.NextPickHostOrderCorrelationId.ShouldBeNull();
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(dropAction.Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(actionGroup.AssignedAgv.GetValueOrDefault());
        droppingAgvPassedDoppelspielTriggerPoint.DroppingHostOrderCorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DropStationLimitExceeded_PublishesDroppingAgvPassedDoppelspielTriggerPoint()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var agv = AgvGenerator.GenerateBusyAgv();
        var agvData = AgvDataGenerator.GenerateAgvData(agv.CorrelationId);
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: ActionState.Completed, destination: station.Name,
            canRunDoppelspiel: true);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.Order;
        var nextPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: null,
            handlingUnit: "HU",
            pickActionState: ActionState.Created, source: station.Name,
            dropActionState: ActionState.Created, destination: "AP01");
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextPickForStation(nextPickActionGroup)
            .WithStationGroupLimit(false)
            .AddActionGroup(actionGroup)
            .AddAgv(agv);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = await fixture.GetFirstPublishedMessageAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        droppingAgvPassedDoppelspielTriggerPoint.NextPickHostOrderCorrelationId.ShouldBeNull();
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(dropAction.Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(actionGroup.AssignedAgv.GetValueOrDefault());
        droppingAgvPassedDoppelspielTriggerPoint.DroppingHostOrderCorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_DropNextPickAgvMustChargeUnmatchedPick_PublishesDroppingAgvPassedDoppelspielTriggerPointAndQueueActionGroupForUnmatchedPick()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var agv = AgvGenerator.GenerateBusyAgv(batteryCharge: -100);
        var agvData = AgvDataGenerator.GenerateAgvData(agv.CorrelationId);
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: ActionState.Completed, destination: station.Name,
            canRunDoppelspiel: true);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.Order;
        var nextPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: null,
            handlingUnit: "HU",
            pickActionState: ActionState.Created, source: station.Name,
            dropActionState: ActionState.Created, destination: "AP01");
        var unmatchPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            isQueued: false,
            assignedAgv: null,
            handlingUnit: "HU",
            pickActionState: ActionState.Created, source: station.Name,
            dropActionState: ActionState.Created, destination: "AP01");
        unmatchPickActionGroup.RegisterDoppelspiel(ActionType.Pick);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextPickForStation(nextPickActionGroup)
            .WithUnmatchedPick(unmatchPickActionGroup)
            .WithAgvMustCharge()
            .WithStationGroupLimit(false)
            .AddActionGroup(actionGroup)
            .AddAgv(agv);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = await fixture.GetFirstPublishedMessageAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        droppingAgvPassedDoppelspielTriggerPoint.NextPickHostOrderCorrelationId.ShouldBeNull();
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(dropAction.Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DroppingHostOrderCorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        unmatchPickActionGroup.Actions.First(a => a.ActionType == ActionType.Pick).IsRegisteredForDoppelspiel.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDoppelspielTriggerPoint_Doppelspiel_PublishesDroppingAgvPassedDoppelspielTriggerPointAndQueueActionGroup()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var agv = AgvGenerator.GenerateBusyAgv();
        var agvData = AgvDataGenerator.GenerateAgvData(agv.CorrelationId);
        var status = AgvStatusGenerator.Generate(agvCorrelationId: agvData.CorrelationId);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            handlingUnit: "HU",
            pickActionState: ActionState.Completed, source: "APL1",
            dropActionState: ActionState.Completed, destination: station.Name,
            canRunDoppelspiel: true);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.Order;
        var nextPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: null,
            handlingUnit: "HU",
            pickActionState: ActionState.Created, source: station.Name,
            dropActionState: ActionState.Created, destination: "AP01");
        var nextPick = nextPickActionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithAgvMustCharge(false)
            .WithNextPickForStation(nextPickActionGroup)
            .WithStationGroupLimit(true)
            .AddActionGroup(actionGroup)
            .AddAgv(agv);

        var agvPassedDoppelspielTriggerPoint = new AgvPassedDoppelspielTriggerPoint(agvData, status, fixture.ActionGroupAsMessage(actionGroup), [], [], station.Name);

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedDoppelspielTriggerPoint>();

        await fixture.ShouldNotHavePublishedAsync<ManualActionPassedDoppelspielTrigger>();
        await fixture.ShouldNotHavePublishedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        var droppingAgvPassedDoppelspielTriggerPoint = await fixture.GetFirstPublishedMessageAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        droppingAgvPassedDoppelspielTriggerPoint.NextPickHostOrderCorrelationId.ShouldBe(nextPickActionGroup.CorrelationId);
        droppingAgvPassedDoppelspielTriggerPoint.DropActionTarget.Id.ShouldBe(dropAction.Target.Node!.Id);
        droppingAgvPassedDoppelspielTriggerPoint.AgvCorrelationId.ShouldBe(actionGroup.AssignedAgv.GetValueOrDefault());
        droppingAgvPassedDoppelspielTriggerPoint.DroppingHostOrderCorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        dropAction.DoppelspielPartner.ShouldBe(nextPickActionGroup.CorrelationId);
        nextPick.DoppelspielPartner.ShouldBe(actionGroup.CorrelationId);
        nextPickActionGroup.AssignedAgv.ShouldBe(actionGroup.AssignedAgv);
    }

    [TestMethod]
    public async Task Consume_AgvLeftDoppelspielTrigger_NoUnmatchedDoppelspielPick_DoesNotPublishQueueActionGroup()
    {
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithUnmatchedPick(null);

        await fixture.PublishMessageAsync(new AgvLeftDoppelspielTrigger(Guid.NewGuid(), "APL1"));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AgvLeftDoppelspielTrigger_UnmatchedDoppelspielPick_PublishesQueueActionGroup()
    {
        var unmatchPickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            isQueued: false,
            assignedAgv: null,
            handlingUnit: "HU",
            pickActionState: ActionState.Created, source: "APL1");
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithUnmatchedPick(unmatchPickActionGroup);

        await fixture.PublishMessageAsync(new AgvLeftDoppelspielTrigger(Guid.NewGuid(), "APL1"));

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_NoCurrentDropAction_DoesNotUpdateActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), canRunDoppelspiel: false);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithCurrentAgvAction(actionGroup.Actions.First(a => a.ActionType == ActionType.Pick))
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(new EvacuateOrAssignQueueHead("Queue", "Evacuation".ToGuid(), actionGroup.AssignedAgv.GetValueOrDefault(), "APl1"));

        actionGroup.CanRunDoppelspiel.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_ValidData_UpdatesActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), canRunDoppelspiel: false);
        actionGroup.CanRunDoppelspiel.ShouldBeFalse();

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithCurrentAgvAction(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop))
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(new EvacuateOrAssignQueueHead("Queue", "Evacuation".ToGuid(), actionGroup.AssignedAgv.GetValueOrDefault(), "APl1"));

        actionGroup.CanRunDoppelspiel.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_NoCurrentAction_RespondsWithNull()
    {
        var agvCorrelationId = Guid.NewGuid();
        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithCurrentAgvAction(null);

        var message = new TryAssignDelayedOrderToAgv(agvCorrelationId, Guid.NewGuid(), "Station".ToGraphNode());

        await fixture.PublishMessageAsync(message);

        var assignedDelayedOrder = await fixture.GetFirstPublishedMessageAsync<AssignedDelayedOrder>();
        assignedDelayedOrder.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_CurrentActionIsNotDrop_RespondsWithNull()
    {
        var agvCorrelationId = Guid.NewGuid();
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId);
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithCurrentAgvAction(pickAction)
            .AddActionGroup(actionGroup);

        var message = new TryAssignDelayedOrderToAgv(agvCorrelationId, Guid.NewGuid(), "Station".ToGraphNode());

        await fixture.PublishMessageAsync(message);

        var assignedDelayedOrder = await fixture.GetFirstPublishedMessageAsync<AssignedDelayedOrder>();
        assignedDelayedOrder.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_DoppelspielModeNotOrderOrQueueHead_RespondsWithNull()
    {
        var agvCorrelationId = Guid.NewGuid();
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.None;

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithCurrentAgvAction(dropAction)
            .AddActionGroup(actionGroup);

        var message = new TryAssignDelayedOrderToAgv(agvCorrelationId, Guid.NewGuid(), "Station".ToGraphNode());

        await fixture.PublishMessageAsync(message);

        var assignedDelayedOrder = await fixture.GetFirstPublishedMessageAsync<AssignedDelayedOrder>();
        assignedDelayedOrder.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_NoNextPickAvailable_RespondsWithNull()
    {
        var agvCorrelationId = Guid.NewGuid();
        var station = StationGenerator.GetPickDropStation("PS01");
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: agvCorrelationId,
            destination: station.Name);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.OrderOrQueueHead;

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithCurrentAgvAction(dropAction)
            .WithStation(station)
            .WithNextPickForStation(null)
            .AddActionGroup(actionGroup);

        var message = new TryAssignDelayedOrderToAgv(agvCorrelationId, Guid.NewGuid(), "Station".ToGraphNode());

        await fixture.PublishMessageAsync(message);

        var assignedDelayedOrder = await fixture.GetFirstPublishedMessageAsync<AssignedDelayedOrder>();
        assignedDelayedOrder.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_StationLimitationViolated_RespondsWithNull()
    {
        var agvCorrelationId = Guid.NewGuid();
        var station = StationGenerator.GetPickDropStation("PS01");
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: agvCorrelationId,
            destination: station.Name);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.OrderOrQueueHead;

        var pickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            isQueued: false,
            assignedAgv: null,
            source: station.Name);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithCurrentAgvAction(dropAction)
            .WithStation(station)
            .WithNextPickForStation(pickActionGroup)
            .WithStationGroupLimit(false)
            .AddActionGroup(actionGroup);

        var message = new TryAssignDelayedOrderToAgv(agvCorrelationId, Guid.NewGuid(), "Station".ToGraphNode());

        await fixture.PublishMessageAsync(message);

        var assignedDelayedOrder = await fixture.GetFirstPublishedMessageAsync<AssignedDelayedOrder>();
        assignedDelayedOrder.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_ValidData_AssignsPickAndPublishesQueueActionGroup()
    {
        var agvCorrelationId = Guid.NewGuid();
        var assignableAgv = Guid.NewGuid();
        var station = StationGenerator.GetPickDropStation("PS01");
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: agvCorrelationId,
            destination: station.Name);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.Target.DoppelspielMode = DoppelspielMode.OrderOrQueueHead;

        var pickActionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            isQueued: false,
            assignedAgv: null,
            source: station.Name);
        var pickAction = pickActionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithCurrentAgvAction(dropAction)
            .WithStation(station)
            .WithNextPickForStation(pickActionGroup)
            .WithStationGroupLimit(true)
            .AddActionGroup(actionGroup);

        var message = new TryAssignDelayedOrderToAgv(agvCorrelationId, assignableAgv, "Station".ToGraphNode());

        await fixture.PublishMessageAsync(message);

        var assignedDelayedOrder = await fixture.GetFirstPublishedMessageAsync<AssignedDelayedOrder>();
        assignedDelayedOrder.CorrelationId.ShouldBe(pickActionGroup.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();

        pickActionGroup.AssignedAgv.ShouldBe(assignableAgv);
        pickAction.Target.Node.ShouldBe(dropAction.Target.Node);
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_NoDropActionTargetNode_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(assignedAgv: Guid.NewGuid());
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, pickAction.CorrelationId, DateTime.Now));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_NoDelayedOrder_DoesNotPublishQueueActionGroup()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            destination: station.Name);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextPickForStation(null)
            .AddActionGroup(actionGroup);

        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, pickAction.CorrelationId, DateTime.Now));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_DelayedOrderDoppelspielModeNotAlways_DoesNotPublishQueueActionGroup()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            destination: station.Name);
        var delayedOrder = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            isQueued: false,
            source: station.Name);
        var delayedPickAction = delayedOrder.Actions.First(a => a.ActionType == ActionType.Pick);
        delayedPickAction.Target.DoppelspielMode = DoppelspielMode.Order;

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextPickForStation(delayedOrder)
            .AddActionGroup(actionGroup);

        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, pickAction.CorrelationId, DateTime.Now));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_AlwaysDoppelspiel_PublishesQueueActionGroupAndAssignsAgv()
    {
        var station = StationGenerator.GetPickDropStation("PS01");
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            destination: station.Name);
        var delayedOrder = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            isQueued: false,
            assignedAgv: null,
            source: station.Name);
        var delayedPickAction = delayedOrder.Actions.First(a => a.ActionType == ActionType.Pick);
        delayedPickAction.Target.DoppelspielMode = DoppelspielMode.Always;
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .WithNextPickForStation(delayedOrder)
            .WithStationGroupLimit(true)
            .AddActionGroup(actionGroup);

        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, pickAction.CorrelationId, DateTime.Now));

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        delayedOrder.AssignedAgv.ShouldBe(actionGroup.AssignedAgv);
        delayedPickAction.DoppelspielPartner.ShouldBe(actionGroup.CorrelationId);
        dropAction.DoppelspielPartner.ShouldBe(delayedOrder.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_ActionGroupReassignedOrReset_NonCargoActionGroup_DoesNotUnregisterDoppelspiel()
    {
        var actionGroup = Helper.AgvActionGenerator.GenerateChargeActionGroup();

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupReassignedOrReset(actionGroup.CorrelationId, actionGroup.Creator));

        fixture.ActionGroupRepository.Verify(x => x.UpdateAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_ActionGroupReassignedOrReset_PickNotCompleted_DoesNotUnregisterDoppelspiel()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            pickActionState: ActionState.Created);
        actionGroup.RegisterDoppelspiel(ActionType.Drop);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupReassignedOrReset(actionGroup.CorrelationId, actionGroup.Creator));

        fixture.ActionGroupRepository.Verify(x => x.UpdateAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Never);
        actionGroup.IsRegisteredForDoppelspielDrop.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupReassignedOrReset_HasQueuedDoppelspielPartner_DoesNotUnregisterDoppelspiel()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            pickActionState: ActionState.Completed);
        actionGroup.RegisterDoppelspiel(ActionType.Drop);
        actionGroup.SetDoppelspielpartner(Guid.NewGuid(), ActionType.Drop);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupReassignedOrReset(actionGroup.CorrelationId, actionGroup.Creator));

        fixture.ActionGroupRepository.Verify(x => x.UpdateAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Never);
        actionGroup.IsRegisteredForDoppelspielDrop.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupReassignedOrReset_RegisteredWithoutPartner_UnregistersDoppelspiel()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            pickActionState: ActionState.Completed);
        actionGroup.RegisterDoppelspiel(ActionType.Drop);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupReassignedOrReset(actionGroup.CorrelationId, actionGroup.Creator));

        fixture.ActionGroupRepository.Verify(x => x.UpdateAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()), Times.Once);
        actionGroup.IsRegisteredForDoppelspielDrop.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_NonCargoActionGroup_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GenerateChargeActionGroup();

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupCanceled(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, DateTime.Now));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_DropNotRegisteredForDoppelspiel_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithUnmatchedPick(null)
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupCanceled(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, DateTime.Now));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_DropRegisteredWithUnmatchedPick_QueuesUnmatchedPick()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            destination: "PS01");
        actionGroup.RegisterDoppelspiel(ActionType.Drop);

        var unmatchedPick = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            isQueued: false,
            source: "PS01");
        unmatchedPick.RegisterDoppelspiel(ActionType.Pick);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithUnmatchedPick(unmatchedPick)
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupCanceled(fixture.ActionGroupAsMessage(actionGroup), actionGroup.CorrelationId, DateTime.Now));

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        unmatchedPick.Actions.First(a => a.ActionType == ActionType.Pick).IsRegisteredForDoppelspiel.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionAborted_TemporaryAbort_DoesNotPublishQueueActionGroup()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid());
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var dropActionMessage = fixture.Mapper.Map<AgvAction>(dropAction);

        await fixture.PublishMessageAsync(new ActionAborted(actionGroup.CorrelationId, dropActionMessage, true));

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ActionAborted_NonTemporaryAbortWithRegisteredDrop_QueuesUnmatchedPick()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            destination: "PS01");
        actionGroup.RegisterDoppelspiel(ActionType.Drop);

        var unmatchedPick = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            isQueued: false,
            source: "PS01");
        unmatchedPick.RegisterDoppelspiel(ActionType.Pick);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithUnmatchedPick(unmatchedPick)
            .AddActionGroup(actionGroup);

        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        var dropActionMessage = fixture.Mapper.Map<AgvAction>(dropAction);

        await fixture.PublishMessageAsync(new ActionAborted(actionGroup.CorrelationId, dropActionMessage));

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        unmatchedPick.Actions.First(a => a.ActionType == ActionType.Pick).IsRegisteredForDoppelspiel.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_PickAction_DoesNotPublishReserveStation()
    {
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var pickActionMessage = fixture.Mapper.Map<AgvAction>(pickAction);

        await fixture.PublishMessageAsync(new StartActionCommand(actionGroup.AssignedAgv!.Value, "AGV001", pickActionMessage));

        await fixture.ShouldNotHavePublishedAsync<ReserveStation>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_DropActionWithoutTargetNode_DoesNotPublishReserveStation()
    {
        var actionGroup = Helper.AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(assignedAgv: Guid.NewGuid());
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        var dropActionMessage = fixture.Mapper.Map<AgvAction>(dropAction);

        await fixture.PublishMessageAsync(new StartActionCommand(actionGroup.AssignedAgv!.Value, "AGV001", dropActionMessage));

        await fixture.ShouldNotHavePublishedAsync<ReserveStation>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_DropActionAtStationWithoutStationGroup_DoesNotPublishReserveStation()
    {
        var station = StationGenerator.GetPickDropStation("PS01", stationGroup: []);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            destination: station.Name);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .AddActionGroup(actionGroup);

        var dropActionMessage = fixture.Mapper.Map<AgvAction>(dropAction);

        await fixture.PublishMessageAsync(new StartActionCommand(actionGroup.AssignedAgv!.Value, "AGV001", dropActionMessage));

        await fixture.ShouldNotHavePublishedAsync<ReserveStation>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_DropActionAtStationWithStationGroup_PublishesReserveStation()
    {
        var station = StationGenerator.GetPickDropStation("PS01", stationGroup: ["PS02".ToGuid()]);
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: Guid.NewGuid(),
            destination: station.Name);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        ActionGroupTestFixtureBase<DoppelspielCommandHandler> fixture = new DoppelspielCommandHandlerTestFixture()
            .WithStation(station)
            .AddActionGroup(actionGroup);

        var dropActionMessage = fixture.Mapper.Map<AgvAction>(dropAction);

        await fixture.PublishMessageAsync(new StartActionCommand(actionGroup.AssignedAgv!.Value, "AGV001", dropActionMessage));

        await fixture.ShouldHavePublishedAsync<ReserveStation>();
    }
}