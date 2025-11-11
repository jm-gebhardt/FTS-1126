using FluentValidation.Results;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Moq;
using Shouldly;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using AgvMode = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvMode;
using ReassignActionGroup = Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup.ReassignActionGroup;
using Route = Gebhardt.Acc.TransportOrder.Routing.Models.Route;
using SaveActionGroupAbortion = Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup.SaveActionGroupAbortion;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

[TestClass]
public class ActionCommandHandlerTest
{
    private const string ChargeNodeName = "Charge-1";
    private static readonly GraphNode _chargeNode = new(ChargeNodeName.ToGuid(), ChargeNodeName) { Type = AgvGraph.NodeType.Charge };
    private const string ParkNodeName = "Park-1";
    private static readonly GraphNode _parkNode = ParkNodeName.ToGraphNode();
    private const string StationNodeName = "Station-1";
    private static readonly GraphNode _stationNode = new(StationNodeName.ToGuid(), StationNodeName) { Type = AgvGraph.NodeType.PickDrop };

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_ValidData_Publishes_StartActionCommandAndActionGroupChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        var startedAction = actionGroup.Actions.First();
        const bool dockFront = true;
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup)
            .WithNode(startedAction.Target.Node!)
            .WithLastActionDockFront(dockFront);

        var startAction = new StartActionOfActionGroup(startedAction, agv.CorrelationId);

        var startActionCommand = await fixture.PublishAndGetFirstMessageAsync<StartActionOfActionGroup, StartActionCommand>(startAction);

        startActionCommand.Action.InfoToAgv.ShouldNotBeNull();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_FirstPickAction_Publishes_StartActionCommandAndActionGroupChangedActionGroupStarted()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        var pickAction = actionGroup.Actions.First();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup)
            .WithNode(pickAction.Target.Node!)
            .WithLastActionDockFront(false);

        var startAction = new StartActionOfActionGroup(pickAction, agv.CorrelationId);
        await fixture.PublishMessageAsync(startAction);

        await fixture.ShouldHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<ActionGroupStarted>();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_AgvInMessageIsNotAssignedAgv_PublishesStartingActionForAgvFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId");
        var anotherAgv = AgvGenerator.GenerateIdleAgv(index: 2, lastNodeName: "NodeId");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(anotherAgv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup);
        var pickAction = actionGroup.Actions.First();
        var startAction = new StartActionOfActionGroup(pickAction, anotherAgv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<StartingActionForAgvFailed>();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_IncompleteAction_PublishesStartingActionForAgvFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId");
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Completed,
            moveActionState: ActionState.Completed);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup);
        var incompleteAction = actionGroup.Actions.Last();
        var startAction = new StartActionOfActionGroup(incompleteAction, agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<StartingActionForAgvFailed>();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_AgvIsLocked_PublishesStartingActionForAgvFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId", mode: new AgvMode(ModeType.Locked));
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup);
        var pickAction = actionGroup.Actions.First();
        var startAction = new StartActionOfActionGroup(pickAction, agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<StartingActionForAgvFailed>();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_AutomaticAction_AgvIsInUserMode_PublishedStartingActionForAgvFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId", mode: new AgvMode(ModeType.UserMode));
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup);
        var pickAction = actionGroup.Actions.First();
        var startAction = new StartActionOfActionGroup(pickAction, agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();

        await fixture.ShouldHavePublishedAsync<StartingActionForAgvFailed>();
        await fixture.ShouldHavePublishedAsync<PauseActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>(); // via PauseActionGroup

        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_AutomaticAction_AgvIsInMaintenanceMode_NothingPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId", mode: new AgvMode(ModeType.Maintenance));
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup);
        var pickAction = actionGroup.Actions.First();
        var startAction = new StartActionOfActionGroup(pickAction, agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<StartingActionForAgvFailed>();
    }

    [TestMethod]
    public async Task Consume_StartActionOfActionGroup_ManualPickDropAction_AgvIsInUserMode_PublishedStartingActionForAgvFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId", mode: new AgvMode(ModeType.UserMode));
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, isManual: true);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup);
        var pickAction = actionGroup.Actions.First();
        var startAction = new StartActionOfActionGroup(pickAction, agv.CorrelationId);

        await fixture.TestHarness.Bus.Publish(startAction);
        await fixture.ShouldHaveConsumedAsync<StartActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<StartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();

        await fixture.ShouldHavePublishedAsync<StartingActionForAgvFailed>();
        await fixture.ShouldHavePublishedAsync<PauseActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_ValidData_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId");
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, hasUnfinishedAction: true, hasNotStartedAction: true);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_ErrorAgvAutomaticAction_DoesNotPublishNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId");
        agv.HasError = true;
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, hasUnfinishedAction: true, hasNotStartedAction: true);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_ErrorAgvManualAction_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId");
        agv.HasError = true;
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(isManual: true);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, hasUnfinishedAction: true, hasNotStartedAction: true);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_NoUnfinishedAction_NodesSelectedForActionNotPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, hasUnfinishedAction: false, hasNotStartedAction: false);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_AgvIsLocked_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, hasUnfinishedAction: true, hasNotStartedAction: true);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
        await fixture.ShouldNotHavePublishedAsync<StartingActionForAgvFailed>();
    }

    [DataRow(ModeType.Locked, CurrentActionGroupPolicy.Pause, false)]
    [DataRow(ModeType.Locked, CurrentActionGroupPolicy.FinishAction, false)]
    [DataRow(ModeType.Locked, CurrentActionGroupPolicy.FinishActionGroup, true)]
    [DataRow(ModeType.UserMode, CurrentActionGroupPolicy.FinishActionGroup, true)]
    [DataRow(ModeType.UserMode, CurrentActionGroupPolicy.Pause, false)]
    [DataRow(ModeType.UserMode, CurrentActionGroupPolicy.FinishAction, false)]
    [DataRow(ModeType.Maintenance, CurrentActionGroupPolicy.FinishAction, false)]
    [DataRow(ModeType.Maintenance, CurrentActionGroupPolicy.FinishActionGroup, true)]
    [DataRow(ModeType.Maintenance, CurrentActionGroupPolicy.Pause, false)]
    [DataRow(ModeType.Normal, CurrentActionGroupPolicy.FinishAction, true)]
    [DataRow(ModeType.Normal, CurrentActionGroupPolicy.FinishActionGroup, true)]
    [DataRow(ModeType.Normal, CurrentActionGroupPolicy.Pause, true)]
    [TestMethod]
    public async Task Consume_TryStartNextActionInGroup_PendingModeChange_PublishesDependingOnModeAndCurrentActionGroupPolicy(ModeType pendingMode, CurrentActionGroupPolicy currentActionGroupPolicy, bool shouldPublish)
    {
        var mode = new AgvMode(ModeType.Normal, Guid.NewGuid());
        mode.PrepareSwitch(pendingMode, currentActionPolicy: currentActionGroupPolicy);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, hasUnfinishedAction: true, hasNotStartedAction: true);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        (await fixture.TestHarness.Published.Any<NodesSelectedForAction>()).ShouldBe(shouldPublish);
        await fixture.ShouldNotHavePublishedAsync<StartingActionForAgvFailed>();
    }

    [TestMethod]
    public async Task Consume_TryStartNextActionInActionGroup_IncompleteAction_PublishesStartingActionForAgvFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Completed,
            moveActionState: ActionState.Completed);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, hasUnfinishedAction: false, hasNotStartedAction: true);

        var command = new TryStartNextActionInActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNextActionInActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
        await fixture.ShouldHavePublishedAsync<StartingActionForAgvFailed>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupNotCompleteNotChargingAction_PublishesActionGroupChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.First().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_PublishesPickActionCompleted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_NoCargo_DoesNotPublishPickActionCompleted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(false)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<PickActionCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_NoCargo_TriggeredFromWebApi_PublishesPickActionCompleted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(false)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId, ActionGroupCreator.TransportOrderWebApi);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_QueuedActionToDropTarget_EmptyQueueNoReadyAgvs_PublishesTryRetractActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string destination = "destination";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, destination: destination);
        var actionGroupToRetract = AgvActionGenerator.GeneratePickDropActionGroup(source: destination, isQueued: true);
        var emptyQueue = QueueGenerator.GenerateEmptyQueue();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true)
            .WithNextPickAtNode(actionGroupToRetract)
            .WithRoute(emptyQueue.NonEntryNodes)
            .WithQueuesOnRoute([emptyQueue]);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
        await fixture.ShouldHavePublishedAsync<TryRetractActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_ResetActionToDropTarget_PublishesTryRetractActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string destination = "destination";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, destination: destination);
        var actionGroupToRetract = AgvActionGenerator.GeneratePickDropActionGroup(source: destination, isQueued: true, estimatedDuration: TimeSpan.Zero);
        var emptyQueue = QueueGenerator.GenerateEmptyQueue();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true)
            .WithNextPickAtNode(actionGroupToRetract)
            .WithRoute(emptyQueue.NonEntryNodes)
            .WithQueuesOnRoute([emptyQueue]);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
        await fixture.ShouldHavePublishedAsync<TryRetractActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_QueuedActionToDropTarget_NoQueueNoReadyAgvs_PublishesTryRetractActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string destination = "destination";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, destination: destination);
        var actionGroupToRetract = AgvActionGenerator.GeneratePickDropActionGroup(source: destination, isQueued: true);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true)
            .WithNextPickAtNode(actionGroupToRetract)
            .WithRoute([Guid.NewGuid()])
            .WithQueuesOnRoute([]);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
        await fixture.ShouldHavePublishedAsync<TryRetractActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_NoActionToDropTarget_DoesNotPublishTryRetractActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string destination = "destination";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, destination: destination);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true)
            .WithNextPickAtNode(null)
            .WithRoute([Guid.NewGuid()])
            .WithQueuesOnRoute([]);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<TryRetractActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_InCompleteDrop_DoesNotPublishTryRetractActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<TryRetractActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_QueuedActionToDropTarget_ReadyAgvs_DoesNotPublishTryRetractActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string destination = "destination";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, destination: destination);
        var actionGroupToRetract = AgvActionGenerator.GeneratePickDropActionGroup(source: destination, isQueued: true);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true)
            .WithNextPickAtNode(actionGroupToRetract)
            .WithRoute([Guid.NewGuid()])
            .WithQueuesOnRoute([])
            .WithReadyAgvs([AgvGenerator.GenerateIdleAgv(2)]);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<TryRetractActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_PickAction_QueuedActionToDropTarget_NonEmptyQueue_DoesNotPublishTryRetractActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string destination = "destination";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, destination: destination);
        var actionGroupToRetract = AgvActionGenerator.GeneratePickDropActionGroup(source: destination, isQueued: true);
        var queue = QueueGenerator.GenerateFullQueue();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true)
            .WithNextPickAtNode(actionGroupToRetract)
            .WithRoute(queue.NonEntryNodes)
            .WithQueuesOnRoute([queue]);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<PickActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<TryRetractActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_NotPickAction_DoesNotPublishPickActionCompleted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(false)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<PickActionCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_DropAction_HasCargo_DoesNotPublishDropActionCompleted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<DropActionCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_DropAction_HasCargo_TriggeredFromWebApi_PublishesDropActionCompleted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, actionGroup.CorrelationId, ActionGroupCreator.TransportOrderWebApi);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<DropActionCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupNotCompleteChargingAction_PublishesActionGroupChangedSetIdleNotCalled()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId");
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.First().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupNotComplete_DoesNotPublishActionGroupCompleted()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started, dropActionState: ActionState.Created, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup);
        var command = new CompleteActionOfActionGroup(actionGroup.Actions.First().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCompleted>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupComplete_PublishesActionGroupCompleted()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Started, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(false)
            .AddActionGroup(actionGroup);
        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Last().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCompleted>();
        await fixture.ShouldHavePublishedAsync<TriggerDoppelspielPartnerQueueing>();
    }

    [DataRow(CurrentActionGroupPolicy.Pause, false)]
    [DataRow(CurrentActionGroupPolicy.FinishActionGroup, false)]
    [DataRow(CurrentActionGroupPolicy.FinishAction, true)]
    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupNotComplete_AgvHasPendingModeSwitch_PublishesSwitchModeDependingOnCurrentActionPolicy(CurrentActionGroupPolicy policy, bool shouldPublish)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(ModeType.Maintenance, currentActionPolicy: policy);
        var agv = AgvGenerator.GenerateBusyAgv(mode: mode);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started, dropActionState: ActionState.Created, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup)
            .WithCurrentActionGroupsToTarget([]);
        var command = new CompleteActionOfActionGroup(actionGroup.Actions.First().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        (await fixture.TestHarness.Published.Any<SwitchMode>()).ShouldBe(shouldPublish);
    }

    [DataRow(CurrentActionGroupPolicy.Pause, false)]
    [DataRow(CurrentActionGroupPolicy.FinishActionGroup, true)]
    [DataRow(CurrentActionGroupPolicy.FinishAction, true)]
    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupComplete_AgvHasPendingModeSwitch_PublishesSwitchModeDependingOnCurrentActionPolicy(CurrentActionGroupPolicy policy, bool shouldPublish)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(ModeType.Maintenance, currentActionPolicy: policy);
        var agv = AgvGenerator.GenerateBusyAgv(mode: mode);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Completed, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(true)
            .AddActionGroup(actionGroup)
            .WithCurrentActionGroupsToTarget([]);
        var command = new CompleteActionOfActionGroup(actionGroup.Actions.First().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        (await fixture.TestHarness.Published.Any<SwitchMode>()).ShouldBe(shouldPublish);
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_ActionGroupNotPausedAgvCanNotStartAutomaticAction_PublishesPauseActionGroup()
    {
        var agv = AgvGenerator.GenerateBusyAgv(mode: new AgvMode(ModeType.Maintenance));
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Started, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(false)
            .AddActionGroup(actionGroup);

        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Last().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCompleted>();
        await fixture.ShouldHavePublishedAsync<PauseActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CompleteActionOfActionGroup_AgvIsQueueingActionIsTargetingNodeActionGroupComplete_PublishesTryEnqueueAgv()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        agv.IsQueueing = true;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Started, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .WithHasCargo(false)
            .AddActionGroup(actionGroup);
        var command = new CompleteActionOfActionGroup(actionGroup.Actions.Last().CorrelationId, actionGroup.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCompleted>();
        await fixture.ShouldHavePublishedAsync<TryEnqueueAgv>();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_ValidData_PublishesActionGroupCanceled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        var command = new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CancelActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCancelFailed>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_ValidData_RespondsActionGroupCanceled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        Response<ActionGroupCanceled> response = await fixture.GetResponseAsync<ActionGroupCanceled, CancelActionGroup>(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_UnknownActionGroup_PublishesActionGroupCancelFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv);

        var command = new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CancelActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCancelFailed>();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_UnknownActionGroup_RespondsActionGroupCancelFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv);

        Response<ActionGroupCancelFailed> response = await fixture.GetResponseAsync<ActionGroupCancelFailed, CancelActionGroup>(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        response.Message.ErrorCode.ShouldBe(HostOrderCancelFailedErrorCode.OrderDoesNotExist);
        await fixture.ShouldHavePublishedAsync<ActionGroupCancelFailed>();
    }

    [TestMethod]
    [DataRow(nameof(ActionGroupState.Started))]
    [DataRow(nameof(ActionGroupState.Completed))]
    [DataRow(nameof(ActionGroupState.Failed))]
    [DataRow(nameof(ActionGroupState.Canceled))]
    public async Task Consume_CancelActionGroup_InvalidState_PublishesActionGroupCancelFailed(string invalidState)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        actionGroup.GetType().GetProperty(nameof(actionGroup.State))!.SetValue(actionGroup, Enum.Parse<ActionGroupState>(invalidState));
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        var command = new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CancelActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCancelFailed>();
    }

    [TestMethod]
    [DataRow(nameof(ActionGroupState.Started), HostOrderCancelFailedErrorCode.OrderAlreadyStarted)]
    [DataRow(nameof(ActionGroupState.Completed), HostOrderCancelFailedErrorCode.OrderAlreadyFinished)]
    [DataRow(nameof(ActionGroupState.Failed), HostOrderCancelFailedErrorCode.OrderAlreadyFailed)]
    [DataRow(nameof(ActionGroupState.Canceled), HostOrderCancelFailedErrorCode.OrderAlreadyCanceled)]
    public async Task Consume_CancelActionGroup_InvalidState_RespondsActionGroupCancelFailed(string invalidState, HostOrderCancelFailedErrorCode error)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        actionGroup.GetType().GetProperty(nameof(actionGroup.State))!.SetValue(actionGroup, Enum.Parse<ActionGroupState>(invalidState));
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        Response<ActionGroupCancelFailed> response = await fixture.GetResponseAsync<ActionGroupCancelFailed, CancelActionGroup>(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        response.Message.ErrorCode.ShouldBe(error);
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCanceled>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCancelFailed>();
    }

    [TestMethod]
    public async Task Consume_CancelActionGroup_ActionsInInvalidState_PublishesActionGroupCanceled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Invalid, dropActionState: ActionState.Invalid);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);

        var command = new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<CancelActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionGroupCanceled>()).ShouldBeTrue();
        fixture.ActionGroupRepository.Verify(a => a.UpdateActionAndGroupStateAsync(actionGroup.Actions[0], null, ActionState.Canceled, It.Is<Reason>(r => r.Template.Equals("ActionGroup was canceled manually")), It.IsAny<DateTimeOffset>()));
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_ValidData_PublishesActionAbortedAndActionGroupChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);
        var expectedAbortedAction = actionGroup.Actions.First(a => a.State == ActionState.Started);

        var abortedMessage = await fixture.PublishAndGetFirstMessageAsync<AbortActionGroup, ActionAborted>(
            new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        abortedMessage.CorrelationId.ShouldBe(agv.CorrelationId);
        abortedMessage.Action.ActionType.ShouldBe(expectedAbortedAction.ActionType);
        abortedMessage.Action.CorrelationId.ShouldBe(expectedAbortedAction.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<AutomaticActionWithdrawn>();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_ChargeActionGroup_PublishesActionAbortedAndActionGroupChangedAutomaticActionWithdrawn()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId, actionStateMove: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_ParkActionGroup_PublishesActionAbortedAndActionGroupChangedAutomaticActionWithdrawn()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroupId = Guid.NewGuid();
        var parkAction = AgvActionGenerator.GenerateParkAction(parentId: actionGroupId, actionState: ActionState.Started);
        var actionGroup = new ActionGroup(actionGroupId,
            string.Empty,
            new List<AgvAction> { parkAction },
            ActionGroupCreator.TransportOrder,
            createdDate: DateTime.UtcNow, assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Park);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_NoAssignedAgv_DoesNotPublishActionAborted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_MoveActionSkippedAndDropRunning_PublishesActionAbortedForRunningAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);

        var actionGroup = AgvActionGenerator.GeneratePickMoveDropActionGroup(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Completed,
            wasMoveSkipped: true,
            dropActionState: ActionState.Started);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.WebApi));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        var actionAborted = await fixture.GetFirstPublishedMessageAsync<ActionAborted>();
        actionAborted.Action.CorrelationId.ShouldBe(dropAction.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_AllActionsCompleted_DoesNotPublishActionAborted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Completed, dropActionState: ActionState.Completed);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup);

        await fixture.TestHarness.Bus.Publish(new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
    }

    [TestMethod]
    public async Task Consume_AbortActionGroup_AllActionsCompletedOrCreated_PublishesActionAbortedAndActionGroupChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Completed, dropActionState: ActionState.Created);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);
        var expectedAbortedAction = actionGroup.Actions.First(a => a.State != ActionState.Completed);

        var abortedMessage = await fixture.PublishAndGetFirstMessageAsync<AbortActionGroup, ActionAborted>(
            new AbortActionGroup(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging));
        await fixture.ShouldHaveConsumedAsync<AbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        abortedMessage.CorrelationId.ShouldBe(agv.CorrelationId);
        abortedMessage.Action.ActionType.ShouldBe(expectedAbortedAction.ActionType);
        abortedMessage.Action.CorrelationId.ShouldBe(expectedAbortedAction.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<AutomaticActionWithdrawn>();
    }

    [TestMethod]
    public async Task Consume_CreateActionGroup_InvalidActionGroup_SavesInvalidActionGroupAndPublishesActionGroupCreatedAndActionGroupInvalid()
    {
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddOrUpdateActionGroupIsUpdating(false);
        const string failureReason = "reason";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.SetupValidationResult(new ValidationResult(new List<ValidationFailure> { new("propertyName", failureReason) }));

        var actionGroupCreatedEvent = await fixture.PublishAndGetFirstMessageAsync<CreateActionGroup, ActionGroupCreated>(
            new CreateActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup)));

        await fixture.ShouldHaveConsumedAsync<CreateActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        actionGroupCreatedEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupCreatedEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);

        await fixture.ShouldHavePublishedAsync<ActionGroupInvalid>();
        var invalidEvent = await fixture.GetFirstPublishedMessageAsync<ActionGroupInvalid>();
        invalidEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        invalidEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);

        var actionGroupInvalidEvent = await fixture.GetFirstPublishedMessageAsync<ActionGroupInvalid>();
        actionGroupInvalidEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupInvalidEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);
        actionGroupInvalidEvent.ValidationErrors.ShouldBe(new List<string> { failureReason });
    }

    [TestMethod]
    public async Task Consume_CreateActionGroup_ActionGroupExists_DoesNotPublishActionGroupCreated()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);
       
        await fixture.PublishMessageAsync(new CreateActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup)));

        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_InvalidActionGroup_DoNotThrowException_SavesInvalidActionGroupAndPublishesActionGroupCreatedAndActionGroupInvalid()
    {
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddOrUpdateActionGroupIsUpdating(false);
        const string failureReason = "reason";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.SetupValidationResult(new ValidationResult(new List<ValidationFailure> { new("propertyName", failureReason) }));

        var actionGroupCreatedEvent = await fixture.PublishAndGetFirstMessageAsync<QueueActionGroup, ActionGroupCreated>(
            new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), false));

        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        actionGroupCreatedEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupCreatedEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);

        await fixture.ShouldHavePublishedAsync<ActionGroupInvalid>();
        var invalidEvent = await fixture.GetFirstPublishedMessageAsync<ActionGroupInvalid>();
        invalidEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        invalidEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);

        var actionGroupInvalidEvent = await fixture.GetFirstPublishedMessageAsync<ActionGroupInvalid>();
        actionGroupInvalidEvent.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupInvalidEvent.ActionGroup.State.ShouldBe(ActionGroupState.Invalid);
        actionGroupInvalidEvent.ValidationErrors.ShouldBe(new List<string> { failureReason });
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_InvalidActionGroupThrowException_DoesNotPublishEventsDoNotSaveActionGroup()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        const string failureReason = "reason";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.SetupValidationResult(new ValidationResult(new List<ValidationFailure> { new("propertyName", failureReason) }));

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), true));

        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupInvalid>();
        fixture.IsActionGroupSaved(Times.Never());
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_AssignedAgv_PublishesActionGroupCreated()
    {
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddOrUpdateActionGroupIsUpdating(false);
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        fixture.SetupValidationResult(new ValidationResult());
        fixture.AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), true));

        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<Success>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupInvalid>();
    }

    [TestMethod]
    public async Task Consume_CreateActionGroup_AssignedAgv_PublishesActionGroupCreated()
    {
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddOrUpdateActionGroupIsUpdating(false);
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        fixture.SetupValidationResult(new ValidationResult());
        fixture.AddAgv(agv);

        await fixture.PublishMessageAsync(new CreateActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup)));

        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupInvalid>();
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_NoAssignedAgv_PublishesActionGroupCreated_DoesNotRespondSuccess()
    {
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddOrUpdateActionGroupIsUpdating(false);
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null);
        fixture.SetupValidationResult(new ValidationResult());
        fixture.AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), false));

        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldNotHavePublishedAsync<Success>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupInvalid>();
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_AssignedActionGroupWithIncompleteFirstAction_NewActionGroup_PublishesActionGroupCreated()
    {
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddOrUpdateActionGroupIsUpdating(false);
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, source: string.Empty);
        fixture.SetupValidationResult(new ValidationResult());
        fixture.AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), true));

        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupInvalid>();
    }

    [TestMethod]
    public async Task Consume_QueueActionGroup_AssignedActionGroupWithIncompleteFirstAction_ExistingActionGroup_PublishesActionGroupChanged()
    {
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddOrUpdateActionGroupIsUpdating();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, source: string.Empty);
        fixture.SetupValidationResult(new ValidationResult());
        fixture.AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new QueueActionGroup(fixture.Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), true));

        await fixture.ShouldHaveConsumedAsync<QueueActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupInvalid>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_BatteryCritical_PublishedActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false, existsPriorToTest: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_AgvLocked_DoesNotPublishActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5, mode: new AgvMode(ModeType.Locked));

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_ActionNotTriggeredByWebApiAndAgvIsInMaintenanceMode_DoesNotPublishActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5, mode: new AgvMode(ModeType.Maintenance));

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode, Creator: ActionGroupCreator.TransportOrder);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_ActionTriggeredByWebApiAndAgvIsInMaintenanceMode_PublishedActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5, mode: new AgvMode(ModeType.Maintenance));

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false, existsPriorToTest: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode, Creator: ActionGroupCreator.TransportOrderWebApi);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_ActionNotTriggeredByWebApiAndAgvIsInUserMode_DoesNotPublishActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5, mode: new AgvMode(ModeType.UserMode));

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode, Creator: ActionGroupCreator.TransportOrder);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_ActionTriggeredByWebApiAndAgvIsInUserMode_PublishedActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5, mode: new AgvMode(ModeType.UserMode));

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false, existsPriorToTest: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode, Creator: ActionGroupCreator.TransportOrderWebApi);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
    }

    [DataRow(ModeType.UserMode)]
    [DataRow(ModeType.Maintenance)]
    [DataRow(ModeType.Locked)]
    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_PendingModeChange_ActionNotTriggeredByWebApi_DoesNotPublishActionGroupCreated(ModeType pendingMode)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(pendingMode);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false, existsPriorToTest: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, "a charger".ToGraphNode(), Creator: ActionGroupCreator.TransportOrder);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [DataRow(ModeType.UserMode)]
    [DataRow(ModeType.Maintenance)]
    [DataRow(ModeType.Locked)]
    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_PendingModeChange_ActionTriggeredByWebApi_PublishesActionGroupCreated(ModeType pendingMode)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(pendingMode);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false, existsPriorToTest: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, "a charger".ToGraphNode(), Creator: ActionGroupCreator.TransportOrderWebApi);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_AgvIsCharging_DoesNotPublishActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateChargingAgv(batteryCharge: 10);

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(chargeActionGroup, hasUnfinishedAction: false)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_BatteryNotCriticalAndExistingActionGroups_DoesNotPublishActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 30);

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var pickAndDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(pickAndDropActionGroup)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_BatteryCriticalAndExistingActionGroups_PublishedActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var pickAndDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful()
            .AddAgv(agv)
            .AddActionGroup(pickAndDropActionGroup)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateChargingActionGroup_AddingActionGroupNotSuccessful_DoesNotPublishActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 5);

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var pickAndDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful(false)
            .AddAgv(agv)
            .AddActionGroup(pickAndDropActionGroup)
            .AddActionGroupRetrieval(headingTo: false);

        var command = new CreateChargingActionGroup(chargeActionGroup.CorrelationId, agv.CorrelationId, _chargeNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateChargingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateParkingActionGroup_ValidAgv_PublishedActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var parkActions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(targetNodeName: ParkNodeName) };
        var parkActionGroup = AgvActionGenerator.GenerateActionGroup(parkActions);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful()
            .AddAgv(agv)
            .AddActionGroup(parkActionGroup, hasUnfinishedAction: false);

        var command = new CreateParkingActionGroup(Guid.NewGuid(), agv.CorrelationId, _parkNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateParkingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateParkingActionGroup_AddingActionGroupNotSuccessful_DoesNotPublishActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var parkActions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(targetNodeName: ParkNodeName) };
        var parkActionGroup = AgvActionGenerator.GenerateActionGroup(parkActions);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful(false)
            .AddAgv(agv)
            .AddActionGroup(parkActionGroup, hasUnfinishedAction: false);

        var command = new CreateParkingActionGroup(Guid.NewGuid(), agv.CorrelationId, _parkNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateParkingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [DataRow(ModeType.Maintenance)]
    [DataRow(ModeType.UserMode)]
    [DataRow(ModeType.Locked)]
    [TestMethod]
    public async Task Consume_CreateParkingActionGroup_PendingModeChange_DoesNotPublishActionGroupCreated(ModeType pendingMode)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(pendingMode);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv);

        var command = new CreateParkingActionGroup(Guid.NewGuid(), agv.CorrelationId, _parkNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateParkingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_CreateQueueingActionGroup_ValidAgv_PublishedActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string queueNodeName = "Park-1";

        var queueActions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(targetNodeName: queueNodeName) };
        var queueActionGroup = AgvActionGenerator.GenerateActionGroup(queueActions);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithAddActionGroupIsSuccessful()
            .AddAgv(agv)
            .AddActionGroup(queueActionGroup, hasUnfinishedAction: false);

        var command = new CreateQueueingActionGroup(Guid.NewGuid(), agv.CorrelationId, _parkNode);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
    }

    [DataRow(ModeType.Maintenance)]
    [DataRow(ModeType.UserMode)]
    [DataRow(ModeType.Locked)]
    [TestMethod]
    public async Task Consume_CreateQueueingActionGroup_PendingModeChange_DoesNotPublishActionGroupCreated(ModeType pendingMode)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(pendingMode);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);
        const string queueNodeName = "Node-1";

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv);

        var command = new CreateQueueingActionGroup(Guid.NewGuid(), agv.CorrelationId, queueNodeName.ToGraphNode());
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvMustCharge_NodesSelectedForActionNotPublished()
    {
        var fixture = new ActionCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: fixture.SettingsReadModel.BatteryLevelCritical - 1);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        fixture.AddAgv(agv);
        fixture.AddActionGroup(actionGroup, hasUnfinishedAction: false);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvIsLocked_NodesSelectedForActionNotPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvHasUnfinishedDropAction_NodesSelectedForActionNotPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(ignoreLocked: false);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: true);

        await fixture.PublishMessageAsync(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_ValidData_PublishesNodesSelectedForAction(bool lastActionWasMoveToCoordinate)
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "A node");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null);
        string[] nodes = ["Node1", "Node2"];

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false)
            .AddRoute(nodes, edges: [Guid.NewGuid()])
            .WithLastActionIsMoveToCoordinate(lastActionWasMoveToCoordinate);

        await fixture.PublishMessageAsync(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
        var nodesSelectedForAction = await fixture.GetFirstPublishedMessageAsync<NodesSelectedForAction>();
        nodesSelectedForAction.Action.Nodes.Select(n => n.Name).ToArray().ShouldBeEquivalentTo(nodes);
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_MoveToCoordinates_PublishesNodesSelectedForAction(bool lastActionWasMoveToCoordinate)
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "A node");
        var actionGroup = AgvActionGenerator.GenerateMoveToCoordinateActionGroup(targetPosition: new AgvPosition(true, 1, 1, 1, 1), assignedAgv: agv.CorrelationId);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false)
            .WithLastActionIsMoveToCoordinate(lastActionWasMoveToCoordinate);

        await fixture.PublishMessageAsync(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
        var nodesSelectedForAction = await fixture.GetFirstPublishedMessageAsync<NodesSelectedForAction>();
        nodesSelectedForAction.Action.Nodes.ShouldBeEmpty();
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvHasErrorManualActionGroup_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "A node");
        agv.HasError = true;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, isManual: true);
       
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false, isManual: true)
            .AddRoute(["A node", actionGroup.Actions.First().Target.Node!.Name], edges: [Guid.NewGuid()]);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvHasErrorAutomaticActionGroup_DoesNotPublishNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "A node");
        agv.HasError = true;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false)
            .AddRoute(["A node", actionGroup.Actions.First().Target.Node!.Name], edges: [Guid.NewGuid()]);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
        actionGroup.AssignedAgv.ShouldNotBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_NoActionGroup_PublishesAgvCanStartIdleAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "A node");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false, existsPriorToTest: false);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
        await fixture.ShouldHavePublishedAsync<AgvCanStartIdleAction>();
        actionGroup.AssignedAgv.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_AgvIsNotAvailable_DoesNotPublishNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, hasUnfinishedAction: false)
            .AddRoute(["A node", actionGroup.Actions.First().Target.Node!.Name], edges: [Guid.NewGuid()]);

        await fixture.TestHarness.Bus.Publish(new TryStartNewActionGroup(agv.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
        actionGroup.AssignedAgv.ShouldNotBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_MaintenanceMode_ValidData_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId", mode: new AgvMode(ModeType.Maintenance));
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, isManual: true);

        var command = new TryStartNewActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
        fixture.VerifyGetNextManualActionGroupForAgvAsyncInput(agv.CorrelationId, [ActionType.Pick, ActionType.Drop, ActionType.Charge, ActionType.Move]);
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_UserMode_PublishesNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId", mode: new AgvMode(ModeType.UserMode));
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddRoute(nodes: ["Node1", "Node2"], edges: [Guid.NewGuid()])
            .AddActionGroup(actionGroup, isManual: true);

        var command = new TryStartNewActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldHavePublishedAsync<NodesSelectedForAction>();
        fixture.VerifyGetNextManualActionGroupForAgvAsyncInput(agv.CorrelationId, [ActionType.Charge, ActionType.Move]);
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_ShouldStartManualAction_NoManualActionGroup_DoesNotPublishNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId", mode: new AgvMode(ModeType.UserMode));
        agv.SwitchMode();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, existsPriorToTest: false);

        var command = new TryStartNewActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
        fixture.VerifyGetNextManualActionGroupForAgvAsyncInput(agv.CorrelationId, [ActionType.Charge, ActionType.Move]);
    }

    [TestMethod]
    public async Task Consume_TryStartNewActionGroup_ShouldStartManualAction_NoUnStartedAction_DoesNotPublishNodesSelectedForAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "NodeId", mode: new AgvMode(ModeType.UserMode));
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        actionGroup.StartAction(actionGroup.Actions.First().CorrelationId, [], [], DateTimeOffset.Now);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup, existsPriorToTest: true);

        var command = new TryStartNewActionGroup(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryStartNewActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodesSelectedForAction>();
        fixture.VerifyGetNextManualActionGroupForAgvAsyncInput(agv.CorrelationId, [ActionType.Charge, ActionType.Move]);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Consume_ResetActionGroup_StartedPickDropActionGroup_PublishesActionAbortedActionGroupChangedRespondsSuccess(bool keepAgv)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(true);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);
        var expectedResetAction = actionGroup.Actions.First(a => a.State == ActionState.Started);

        await fixture.PublishMessageAsync(new ResetActionGroup(actionGroup.CorrelationId, keepAgv));

        var abortedMessage = await fixture.GetFirstPublishedMessageAsync<ActionAborted>();
        abortedMessage.CorrelationId.ShouldBe(agv.CorrelationId);
        abortedMessage.Action.ActionType.ShouldBe(expectedResetAction.ActionType);
        abortedMessage.Action.CorrelationId.ShouldBe(expectedResetAction.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<Success>();
        await fixture.ShouldNotHavePublishedAsync<AutomaticActionWithdrawn>();
        await fixture.ShouldHavePublishedAsync<ActionGroupReassignedOrReset>();
        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.AssignedAgv.ShouldBe(keepAgv ? agv.CorrelationId : null);
        actionGroup.StartedDate.ShouldBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Consume_ResetActionGroup_CompletedPickDropActionGroup_PublishesActionGroupChangedRespondsSuccess(bool keepAgv)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TryTransitionToBusy(false);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Completed, dropActionState: ActionState.Completed);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);

        await fixture.PublishMessageAsync(new ResetActionGroup(actionGroup.CorrelationId, keepAgv));

        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<Success>();
        await fixture.ShouldHavePublishedAsync<ActionGroupReassignedOrReset>();
        await fixture.ShouldNotHavePublishedAsync<AutomaticActionWithdrawn>();

        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.AssignedAgv.ShouldBe(keepAgv ? agv.CorrelationId : null);
        actionGroup.StartedDate.ShouldBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_ResetActionGroup_ActionGroupWithNodeReservation_PublishesAutomaticActionWithdrawn()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Charge);

        await fixture.PublishMessageAsync(new ResetActionGroup(actionGroup.CorrelationId, false));

        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<Success>();
        await fixture.ShouldHavePublishedAsync<ActionGroupReassignedOrReset>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();

        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.AssignedAgv.ShouldBeNull();
        actionGroup.StartedDate.ShouldBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_ValidData_PublishesAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Completed,
            moveActionState: ActionState.Completed);

        var targetNode = "target".ToGraphNode();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .WithNode(targetNode);

        var incompleteAction = actionGroup.Actions.First(a => a.IsIncomplete);

        await fixture.PublishMessageAsync(new SetTargetForAction(incompleteAction.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, targetNode.Id));

        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        incompleteAction.IsIncomplete.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_AssignedAgv_PublishesAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Completed,
            moveActionState: ActionState.Completed);
        var targetNode = "target".ToGraphNode();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .WithNode(targetNode);

        var incompleteAction = actionGroup.Actions.First(a => a.IsIncomplete);

        await fixture.PublishMessageAsync(new SetTargetForAction(incompleteAction.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, targetNode.Id));

        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        incompleteAction.IsIncomplete.ShouldBeFalse();
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_NoAssignedAgv_DoesNotPublishAgvBecameIdle()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: null,
            pickActionState: ActionState.Completed,
            moveActionState: ActionState.Completed);

        var targetNode = "target".ToGraphNode();

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .WithNode(targetNode);

        var incompleteAction = actionGroup.Actions.First(a => a.IsIncomplete);

        await fixture.PublishMessageAsync(new SetTargetForAction(incompleteAction.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, targetNode.Id));

        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        incompleteAction.IsIncomplete.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_CreatedActionGroup_DoesNotPublishAgvBecameIdle()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: null,
            pickActionState: ActionState.Created,
            moveActionState: ActionState.Created);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .WithNode(_parkNode);

        var incompleteAction = actionGroup.Actions.First(a => a.IsIncomplete);

        await fixture.PublishMessageAsync(new SetTargetForAction(incompleteAction.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _parkNode.Id));
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        fixture.ActionGroupRepository.Verify(r => r.UpdateAsync(It.Is<ActionGroup>(ag =>
            ag.CorrelationId == actionGroup.CorrelationId &&
            ag.Actions[2].Target.Node!.Name == _parkNode.Name)), Times.Once);

        incompleteAction.IsIncomplete.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_Update_StartFromCharger_DockfrontSet()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Created,
            moveActionState: ActionState.Started);
        actionGroup.Actions[0].InfoToAgv = new Dictionary<InfoToAgvKey, string>();
        var nodes = new List<GraphNode> { _chargeNode, "Node2".ToGraphNode() };
        var edges = new[] { Guid.NewGuid() };
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddAgv(agv)
            .WithNode(nodes.Concat([_stationNode]).ToArray())
            .AddRoute(nodes: nodes.Skip(1).Select(n => n.Name).Concat([_stationNode.Name]).ToArray(), edges: edges)
            .WithLastActionDockFront(true);

        var incompleteAction = actionGroup.Actions.First(a => a.IsIncomplete);
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        const bool moveDockFront = false;
        moveAction.Edges = edges;
        moveAction.Nodes = nodes.ToArray();
        moveAction.SetDockFrontValue(moveDockFront);

        await fixture.PublishMessageAsync(new SetTargetForAction(incompleteAction.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _stationNode.Id));

        await fixture.ShouldHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        var updateActionCommand = fixture.TestHarness.Published.Select<UpdateActionCommand>().First().Context.Message;
        updateActionCommand.Action.CorrelationId.ShouldBe(moveAction.CorrelationId);
        updateActionCommand.Action.InfoToAgv!.GetDockFront().ShouldBe(moveDockFront);
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_AssignedAgvWithUpdate_PublishesUpdateActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Completed,
            moveActionState: ActionState.Started);
        actionGroup.Actions[0].InfoToAgv = new Dictionary<InfoToAgvKey, string>();
        var nodes = new List<string> { "Node1", "Node2" };
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddAgv(agv)
            .WithNode(nodes.Select(n => n.ToGraphNode()).Concat([_parkNode]).ToArray())
            .AddRoute(nodes: nodes.ToArray(), edges: [Guid.NewGuid()]);

        var incompleteAction = actionGroup.Actions.First(a => a.IsIncomplete);
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);

        await fixture.PublishMessageAsync(new SetTargetForAction(incompleteAction.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _parkNode.Id));

        await fixture.ShouldHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        incompleteAction.IsIncomplete.ShouldBeFalse();
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
        actionGroup.Actions[1].Target.Node.ShouldNotBeNull();
        actionGroup.Actions[1].Target.Node.ShouldNotBeNull();
        actionGroup.Actions[1].Target.Node!.Name.ShouldBe(_parkNode.Name);
        actionGroup.Actions[1].ActionType.ShouldBe(ActionType.Drop);

        var updateActionCommand = fixture.TestHarness.Published.Select<UpdateActionCommand>().First().Context.Message;
        updateActionCommand.Action.CorrelationId.ShouldBe(moveAction.CorrelationId);
        updateActionCommand.Action.Nodes.Select(n => n.Name).ToList().ShouldBeEquivalentTo(nodes);
        moveAction.Nodes.Select(n => n.Name).ToList().ShouldBeEquivalentTo(nodes);
        moveAction.ActionType.ShouldBe(incompleteAction.ActionType);
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_TargetResetForStartedAction_PublishesUpdateActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Started);
        actionGroup.Actions[0].InfoToAgv = new Dictionary<InfoToAgvKey, string>();
        string[] nodes = ["Node1", "Node2"];
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddAgv(agv)
            .WithNode(nodes.Select(n => n.ToGraphNode()).Concat([_parkNode]).ToArray())
            .AddRoute(nodes: nodes, edges: [Guid.NewGuid()]);

        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        await fixture.PublishMessageAsync(new SetTargetForAction(pickAction.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _parkNode.Id));

        await fixture.ShouldHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        pickAction.IsIncomplete.ShouldBeFalse();
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
        pickAction.Target.Node.ShouldNotBeNull();
        pickAction.Target.Node.Name.ShouldBe(_parkNode.Name);

        var updateActionCommand = fixture.TestHarness.Published.Select<UpdateActionCommand>().First().Context.Message;
        updateActionCommand.Action.CorrelationId.ShouldBe(pickAction.CorrelationId);
    }

    [DataRow(ActionType.Pick, ActionState.Started)]
    [DataRow(ActionType.Drop, ActionState.Started)]
    [TestMethod]
    public async Task Consume_SetTargetForAction_TargetResetForStartedAction_OnlyPickOrDrop_PublishesUpdateActionGroup(ActionType actionType, ActionState state)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var orderId = Guid.NewGuid();
        var testActions = new List<AgvAction>
        {
            new(Guid.NewGuid(), actionType, "node".ToGraphNode(), orderId)
            {
                Sequence = 0,
                ActionType = actionType,
                InfoToAgv = new Dictionary<InfoToAgvKey, string>(),
                Target = new ActionTarget("old target".ToGraphNode())
            }
        };

        var actionGroup = new ActionGroup(
            orderId,
            orderId.ToString(),
            testActions,
            ActionGroupCreator.TransportOrderWebApi,
            Priority.Default,
            false,
            true,
            DateTime.UtcNow,
            agv.CorrelationId);
        if (state == ActionState.Started)
        {
            actionGroup.StartAction(actionGroup.Actions.Single().CorrelationId, [], [], DateTimeOffset.Now);
        }
        string[] nodes = ["Node1", "Node2"];
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddAgv(agv)
            .WithNode(nodes.Select(n => n.ToGraphNode()).Concat([_parkNode]).ToArray())
            .AddRoute(nodes: nodes, edges: [Guid.NewGuid()]);

        var action = actionGroup.Actions.First();

        await fixture.PublishMessageAsync(new SetTargetForAction(action.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _parkNode.Id));

        await fixture.ShouldHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        action.IsIncomplete.ShouldBeFalse();
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
        action.Target.Node.ShouldNotBeNull();
        action.Target.Node.Name.ShouldBe(_parkNode.Name);

        var updateActionCommand = fixture.TestHarness.Published.Select<UpdateActionCommand>().First().Context.Message;
        updateActionCommand.Action.CorrelationId.ShouldBe(action.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_AssignedAgvAllStationsLocked_PublishesAgvBecameIdle_NoUpdate()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Completed,
            moveActionState: ActionState.Started);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .WithLockedStations()
            .AddAgv(agv)
            .WithNode(_parkNode);

        var incompleteAction = actionGroup.Actions.First(a => a.IsIncomplete);

        await fixture.PublishMessageAsync(new SetTargetForAction(incompleteAction.CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _parkNode.Id));

        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        incompleteAction.IsIncomplete.ShouldBeFalse();
        actionGroup.AssignedAgv.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_TargetSetForIncompleteActionAndMoveRunning_SendsUpdate()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(
            assignedAgv: agv.CorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string>(),
            pickActionState: ActionState.Completed,
            moveActionState: ActionState.Started,
            dropActionState: ActionState.Created);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddAgv(agv)
            .WithNode(_parkNode);

        await fixture.PublishMessageAsync(new SetTargetForAction(actionGroup.Actions[2].CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _parkNode.Id));
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        fixture.ActionGroupRepository.Verify(r => r.UpdateAsync(It.Is<ActionGroup>(ag =>
            ag.CorrelationId == actionGroup.CorrelationId &&
            ag.Actions[1].Target.Node!.Name == _parkNode.Name &&
            ag.Actions[2].ShouldSkip
        )), Times.Once);
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_TargetSetAndPreviousRunning_DoesNotSendUpdate()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Started,
            dropActionState: ActionState.Created);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddAgv(agv)
            .WithNode(_stationNode);

        await fixture.PublishMessageAsync(new SetTargetForAction(actionGroup.Actions[1].CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _stationNode.Id));
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        fixture.ActionGroupRepository.Verify(r => r.UpdateAsync(It.Is<ActionGroup>(ag =>
            ag.CorrelationId == actionGroup.CorrelationId &&
            ag.Actions[1].Target.Node!.Name == _stationNode.Name
        )), Times.Once);
    }

    [TestMethod]
    public async Task Consume_SetTargetForAction_TargetSetForRunningAction_SendsUpdate()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: agv.CorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string>(),
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started);

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddAgv(agv)
            .WithNode(_stationNode);

        await fixture.PublishMessageAsync(new SetTargetForAction(actionGroup.Actions[1].CorrelationId, actionGroup.CorrelationId, actionGroup.AssignedAgv, _stationNode.Id));
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        fixture.ActionGroupRepository.Verify(r => r.UpdateAsync(It.Is<ActionGroup>(ag =>
            ag.CorrelationId == actionGroup.CorrelationId &&
            ag.Actions[1].Target.Node!.Name == _stationNode.Name
        )), Times.Once);
    }

    [TestMethod]
    public async Task Consume_PauseActionGroup_UnstartedActionGroup_DoesNotPauseActionGroup()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);

        await fixture.PublishMessageAsync(new PauseActionGroup(actionGroup.CorrelationId, PauseReason.MaintenanceMode));

        actionGroup.State.ShouldNotBe(ActionGroupState.Paused);
        actionGroup.Actions[0].State.ShouldNotBe(ActionState.Paused);
    }

    [TestMethod]
    public async Task Consume_PauseActionGroup_StartedActionGroup_PausesActionGroup()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);

        await fixture.PublishMessageAsync(new PauseActionGroup(actionGroup.CorrelationId, PauseReason.MaintenanceMode));
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        actionGroup.State.ShouldBe(ActionGroupState.Paused);
        actionGroup.Actions[0].State.ShouldBe(ActionState.Paused);
    }

    [TestMethod]
    public async Task Consume_PauseActionGroup_ActionGroupWithCompletedAction_PausesActionGroup()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        actionGroup.CompleteAction(actionGroup.Actions[0].CorrelationId, DateTimeOffset.Now);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);

        await fixture.PublishMessageAsync(new PauseActionGroup(actionGroup.CorrelationId, PauseReason.MaintenanceMode));
        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
        actionGroup.State.ShouldBe(ActionGroupState.Paused);
        actionGroup.Actions[1].State.ShouldBe(ActionState.Paused);
    }

    [DataRow(CurrentActionGroupPolicy.FinishActionGroup, false)]
    [DataRow(CurrentActionGroupPolicy.FinishAction, false)]
    [DataRow(CurrentActionGroupPolicy.Pause, true)]
    [TestMethod]
    public async Task Consume_PendingAgvModeChange_PublishesSwitchModeDependingOnCurrentActionGroupPolicy(CurrentActionGroupPolicy policy, bool shouldPublish)
    {
        var mode = new AgvMode(ModeType.Normal);
        mode.PrepareSwitch(ModeType.Maintenance, currentActionPolicy: policy);
        var agv = AgvGenerator.GenerateBusyAgv(mode: mode);

        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);

        await fixture.PublishMessageAsync(new PauseActionGroup(actionGroup.CorrelationId, PauseReason.MaintenanceMode));
        (await fixture.TestHarness.Published.Any<SwitchMode>()).ShouldBe(shouldPublish);
    }

    [TestMethod]
    public async Task Consume_ResumeActionGroup_ActionGroup_ResumesActionGroupPublishesActionGroupResumed()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId, actionStateMove: ActionState.Paused);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ResumeActionGroup(actionGroup.CorrelationId, null));

        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).State.ShouldBe(ActionState.Created);

        await fixture.ShouldHavePublishedAsync<ActionGroupResumed>();
    }

    [TestMethod]
    public async Task CancelOrAbortActionGroup_StartedActionGroup_ResetIsQueueingAndPublishAbortActionGroup()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        agv.IsQueueing = true;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Started,
            dropActionState: ActionState.Created,
            assignedAgv: agv.CorrelationId,
            orderId: "Queuing"
        );

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddAgv(agv)
            .AddActionGroup(actionGroup)
            .AddActionGroupTargetNodeType(actionGroup, AgvGraph.NodeType.Drop);
        var dbAgv = await fixture.GetAgvAsync(agv.CorrelationId);
        dbAgv.IsQueueing.ShouldBeTrue();

        await fixture.PublishMessageAsync(new CancelOrAbortActionGroup(actionGroup.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
        await fixture.ShouldHavePublishedAsync<AbortActionGroup>();
        dbAgv = await fixture.GetAgvAsync(agv.CorrelationId);
        dbAgv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task SaveActionGroupAbortion_ValidData_PublishesAutomaticActionWithdrawnActionGroupChanged()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: Guid.NewGuid(), actionStateMove: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var command = new SaveActionGroupAbortion(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).CorrelationId, AbortReason.ReactivatedFromCharging);

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<SaveActionGroupAbortion>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHavePublishedAsync<AutomaticActionWithdrawn>();
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_InfinitelyChargingAgvAboveMaxChargeUpIsClosest_PublishesUpdateActionCommand()
    {
        var closestAgv = AgvGenerator.GenerateBusyAgv();
        var route = new Route(42, ["a node".ToGraphNode()], [Guid.NewGuid()]);
        var chargingActionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: closestAgv.CorrelationId, actionStateCharge: ActionState.Started);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithRunningIdleActionGroups([chargingActionGroup])
            .WithRunningChargeActionGroups([chargingActionGroup])
            .AddActionGroup(pickDropActionGroup)
            .WithAgvsWithPriorityActionGroups([])
            .WithErrors([])
            .WithClosestAgv(closestAgv.CorrelationId, route)
            .WithNode(new GraphNode(Guid.NewGuid(), "name"))
            .AddAgvs([closestAgv])
            .WithLastActionDockFront(false);

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AbortAndStartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        var abortAndStartActionCommand = fixture.TestHarness.Published.Select<AbortAndStartActionCommand>().First().Context.Message;
        abortAndStartActionCommand.AbortedActionCorrelationId.ShouldBe(chargingActionGroup.Actions.First(a => a.State == ActionState.Started).CorrelationId);
        abortAndStartActionCommand.Action.Nodes.ShouldBeEquivalentTo(route.Nodes.ToList());
        abortAndStartActionCommand.Action.Edges.ShouldBeEquivalentTo(route.Edges.ToList());
        abortAndStartActionCommand.Action.CorrelationId.ShouldBe(pickDropActionGroup.Actions.First(a => a.ActionType == ActionType.Pick).CorrelationId);
        abortAndStartActionCommand.CorrelationId.ShouldBe(closestAgv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<SaveActionGroupAbortion>();
        var saveActionGroupAbortionCommand = fixture.TestHarness.Published.Select<SaveActionGroupAbortion>().First().Context.Message;
        saveActionGroupAbortionCommand.CorrelationId.ShouldBe(chargingActionGroup.CorrelationId);
        saveActionGroupAbortionCommand.AbortReason.ShouldBe(AbortReason.ReactivatedFromCharging);
        saveActionGroupAbortionCommand.AbortedActionId.ShouldBe(chargingActionGroup.Actions.First(a => a.ActionType == ActionType.Charge).CorrelationId);
        pickDropActionGroup.Priority.ShouldBe(Priority.Default);
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_AgvRunningParkingActionGroupIsClosest_PublishesUpdateActionCommand()
    {
        var closestAgv = AgvGenerator.GenerateBusyAgv();
        var route = new Route(42, ["a node".ToGraphNode()], [Guid.NewGuid()]);
        var parkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Started, assignedAgv: closestAgv.CorrelationId);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithRunningIdleActionGroups([parkingActionGroup])
            .WithRunningChargeActionGroups([])
            .AddActionGroup(pickDropActionGroup)
            .WithAgvsWithPriorityActionGroups([])
            .WithErrors([])
            .WithClosestAgv(closestAgv.CorrelationId, route)
            .WithNode(new GraphNode(Guid.NewGuid(), "name"))
            .AddAgvs([closestAgv])
            .WithLastActionDockFront(false);

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AbortAndStartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        var abortAndStartActionCommand = fixture.TestHarness.Published.Select<AbortAndStartActionCommand>().First().Context.Message;
        abortAndStartActionCommand.AbortedActionCorrelationId.ShouldBe(parkingActionGroup.Actions.First(a => a.State == ActionState.Started).CorrelationId);
        abortAndStartActionCommand.Action.Nodes.ShouldBeEquivalentTo(route.Nodes.ToList());
        abortAndStartActionCommand.Action.Edges.ShouldBeEquivalentTo(route.Edges.ToList());
        abortAndStartActionCommand.Action.CorrelationId.ShouldBe(pickDropActionGroup.Actions.First(a => a.ActionType == ActionType.Pick).CorrelationId);
        abortAndStartActionCommand.CorrelationId.ShouldBe(closestAgv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<SaveActionGroupAbortion>();
        var saveActionGroupAbortionCommand = fixture.TestHarness.Published.Select<SaveActionGroupAbortion>().First().Context.Message;
        saveActionGroupAbortionCommand.CorrelationId.ShouldBe(parkingActionGroup.CorrelationId);
        saveActionGroupAbortionCommand.AbortReason.ShouldBe(AbortReason.ReactivatedFromParking);
        saveActionGroupAbortionCommand.AbortedActionId.ShouldBe(parkingActionGroup.Actions.First(a => a.ActionType == ActionType.Move).CorrelationId);
        pickDropActionGroup.Priority.ShouldBe(Priority.Default);
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_AgvRunningParkingActionGroupIsClosest_HasAlreadyPriorityActionGroup_DoesNotPublishUpdateActionCommand()
    {
        var closestAgv = AgvGenerator.GenerateBusyAgv();
        var route = new Route(42, ["a node".ToGraphNode()], [Guid.NewGuid()]);
        var parkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Started, assignedAgv: closestAgv.CorrelationId);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithRunningIdleActionGroups([parkingActionGroup])
            .WithRunningChargeActionGroups([])
            .AddActionGroup(pickDropActionGroup)
            .WithAgvsWithPriorityActionGroups([closestAgv.CorrelationId])
            .WithClosestAgv(closestAgv.CorrelationId, route)
            .WithErrors([])
            .AddAgvs([closestAgv]);

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldNotHavePublishedAsync<AbortAndStartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<SaveActionGroupAbortion>();
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_AgvRunningParkingActionGroupIsClosest_HasActiveFatalError_DoesNotPublishUpdateActionCommand()
    {
        var closestAgv = AgvGenerator.GenerateBusyAgv();
        var error = AgvErrorGenerator.GenerateAgvError(closestAgv.CorrelationId);
        var route = new Route(42, ["a node".ToGraphNode()], [Guid.NewGuid()]);
        var parkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Started, assignedAgv: closestAgv.CorrelationId);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithRunningIdleActionGroups([parkingActionGroup])
            .AddActionGroup(pickDropActionGroup)
            .WithAgvsWithPriorityActionGroups([])
            .WithErrors([error])
            .WithClosestAgv(closestAgv.CorrelationId, route)
            .AddAgvs([closestAgv]);

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldNotHavePublishedAsync<AbortAndStartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<SaveActionGroupAbortion>();
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_AgvRunningParkingActionGroupIsClosest_HasActiveWarning_PublishesUpdateActionCommand()
    {
        const bool previousDockFront = true;
        var closestAgv = AgvGenerator.GenerateBusyAgv();
        var warning = AgvErrorGenerator.GenerateAgvError(closestAgv.CorrelationId, errorLevel: ErrorLevel.Warning);
        var route = new Route(42, ["a node".ToGraphNode(), "waypoint".ToGraphNode()], [Guid.NewGuid()]);
        var parkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Started, assignedAgv: closestAgv.CorrelationId);
        var startNode = new GraphNode(Guid.NewGuid(), "start") { Type = AgvGraph.NodeType.PickDrop };
        parkingActionGroup.Actions.Single().Nodes = [startNode, "waypoint".ToGraphNode()];
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithRunningIdleActionGroups([parkingActionGroup])
            .WithRunningChargeActionGroups([])
            .AddActionGroup(pickDropActionGroup)
            .WithActionGroupToSaveAbortion(parkingActionGroup)
            .WithAgvsWithPriorityActionGroups([])
            .WithErrors([warning])
            .WithClosestAgv(closestAgv.CorrelationId, route)
            .WithNode(pickDropActionGroup.Actions.Concat(parkingActionGroup.Actions).Select(a => a.Target.Node)
                .Concat([startNode]).ToArray())
            .AddAgvs([closestAgv])
            .WithLastActionDockFront(previousDockFront)
            .WithNode();

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AbortAndStartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        var abortAndStartActionCommand = fixture.TestHarness.Published.Select<AbortAndStartActionCommand>().First().Context.Message;
        abortAndStartActionCommand.AbortedActionCorrelationId.ShouldBe(parkingActionGroup.Actions.First().CorrelationId);
        abortAndStartActionCommand.Action.Nodes.ShouldBeEquivalentTo(route.Nodes.ToList());
        abortAndStartActionCommand.Action.Edges.ShouldBeEquivalentTo(route.Edges.ToList());
        abortAndStartActionCommand.Action.CorrelationId.ShouldBe(pickDropActionGroup.Actions.First(a => a.ActionType == ActionType.Pick).CorrelationId);
        abortAndStartActionCommand.CorrelationId.ShouldBe(closestAgv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<SaveActionGroupAbortion>();
        var saveActionGroupAbortionCommand = fixture.TestHarness.Published.Select<SaveActionGroupAbortion>().First().Context.Message;
        saveActionGroupAbortionCommand.CorrelationId.ShouldBe(parkingActionGroup.CorrelationId);
        saveActionGroupAbortionCommand.AbortReason.ShouldBe(AbortReason.ReactivatedFromParking);
        saveActionGroupAbortionCommand.AbortedActionId.ShouldBe(parkingActionGroup.Actions.First(a => a.ActionType == ActionType.Move).CorrelationId);
        pickDropActionGroup.Priority.ShouldBe(Priority.Default);
        pickDropActionGroup.Actions.Single(a => a.Sequence == 0).InfoToAgv?.GetDockFront().ShouldBe(!previousDockFront); // start of move is station 
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_AgvAtQueueHeadIsClosest_DoesNotPublishUpdateActionCommand()
    {
        var parkingAgv = AgvGenerator.GenerateBusyAgv(index: 2);
        var agvAtQueueHead = AgvGenerator.GenerateBusyAgv();
        var route = new Route(42, ["a node".ToGraphNode()], [Guid.NewGuid()]);
        var parkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Started, assignedAgv: parkingAgv.CorrelationId);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithRunningIdleActionGroups([parkingActionGroup])
            .WithRunningChargeActionGroups([])
            .AddActionGroup(pickDropActionGroup)
            .WithAgvsAtQueueHead([agvAtQueueHead.CorrelationId])
            .WithClosestAgv(agvAtQueueHead.CorrelationId, route)
            .WithAgvsWithPriorityActionGroups([])
            .WithErrors([])
            .AddAgvs([parkingAgv, agvAtQueueHead]);

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<AbortAndStartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<SaveActionGroupAbortion>();
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_ReadyAgvIsClosest_DoesNotPublishUpdateActionCommand()
    {
        var parkingAgv = AgvGenerator.GenerateBusyAgv(index: 2);
        var readyAgv = AgvGenerator.GenerateBusyAgv();
        var route = new Route(42, ["a node".ToGraphNode()], [Guid.NewGuid()]);
        var parkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Started, assignedAgv: parkingAgv.CorrelationId);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithRunningIdleActionGroups([parkingActionGroup])
            .WithRunningChargeActionGroups([])
            .AddActionGroup(pickDropActionGroup)
            .WithClosestAgv(readyAgv.CorrelationId, route)
            .WithAgvsWithPriorityActionGroups([])
            .WithErrors([])
            .AddAgvs([parkingAgv, readyAgv]);

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<AbortAndStartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<SaveActionGroupAbortion>();
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_NoClosestAgv_DoesNotPublishUpdateActionCommand()
    {
        var parkingAgv = AgvGenerator.GenerateBusyAgv(index: 2);
        var readyAgv = AgvGenerator.GenerateBusyAgv();
        var parkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Started, assignedAgv: parkingAgv.CorrelationId);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithRunningIdleActionGroups([parkingActionGroup])
            .WithRunningChargeActionGroups([])
            .AddActionGroup(pickDropActionGroup)
            .WithClosestAgv(null, null)
            .WithAgvsWithPriorityActionGroups([])
            .WithErrors([])
            .AddAgvs([parkingAgv, readyAgv]);

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<UpdateActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<SaveActionGroupAbortion>();
    }

    [TestMethod]
    public async Task TryUpdateIdleAction_NoAvailableAgvs_DoesNotPublishUpdateActionCommand()
    {
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(pickDropActionGroup)
            .WithRunningChargeActionGroups([])
            .WithClosestAgv(null, null)
            .WithAgvsWithPriorityActionGroups([])
            .WithErrors([])
            .AddAgvs([]);

        var command = new TryUpdateIdleAction(pickDropActionGroup.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<AbortAndStartActionCommand>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<SaveActionGroupAbortion>();
    }

    [TestMethod]
    public async Task ReassignActionGroup_NoSelectedAgv_NoAgvAvailable_PublishesActionGroupCreated()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(pickDropActionGroup)
            .WithClosestAgv(null, null)
            .AddAgvs([agv]);

        var command = new ReassignActionGroup(pickDropActionGroup.CorrelationId, null);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<ReassignActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupReassignedOrReset>();
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        pickDropActionGroup.AssignedAgv.ShouldBeNull();
        pickDropActionGroup.Actions.Where(a => !a.ShouldSkip).All(a => a.State == ActionState.Created).ShouldBeTrue();
        pickDropActionGroup.Priority.ShouldBe(Priority.Resume);
    }

    [TestMethod]
    public async Task ReassignActionGroup_NoSelectedAgv_AgvAvailable_Publishes_AgvBecameIdle()
    {
        var readyAgv = AgvGenerator.GenerateIdleAgv();
        var previouslyAssignedAgv = Guid.NewGuid();
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: previouslyAssignedAgv, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(pickDropActionGroup)
            .WithClosestAgv(readyAgv.CorrelationId, null)
            .AddAgvs([readyAgv]);

        var command = new ReassignActionGroup(pickDropActionGroup.CorrelationId, null);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<ReassignActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldHavePublishedAsync<ActionGroupReassignedOrReset>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        pickDropActionGroup.AssignedAgv.ShouldBe(readyAgv.CorrelationId);
        pickDropActionGroup.Actions.Where(a => !a.ShouldSkip).All(a => a.State == ActionState.Created).ShouldBeTrue();
        pickDropActionGroup.Priority.ShouldBe(Priority.Resume);
        pickDropActionGroup.IsManual.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ReassignActionGroup_SelectedAgv_Publishes_AgvBecameIdle()
    {
        var readyAgv = AgvGenerator.GenerateIdleAgv();
        var previouslyAssignedAgv = Guid.NewGuid();
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: previouslyAssignedAgv, pickActionState: ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(pickDropActionGroup)
            .AddAgvs([readyAgv]);

        var command = new ReassignActionGroup(pickDropActionGroup.CorrelationId, readyAgv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<ReassignActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldHavePublishedAsync<ActionGroupReassignedOrReset>();
        await fixture.ShouldNotHavePublishedAsync<ActionGroupCreated>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        pickDropActionGroup.AssignedAgv.ShouldBe(readyAgv.CorrelationId);
        pickDropActionGroup.Actions.Where(a => !a.ShouldSkip).All(a => a.State == ActionState.Created).ShouldBeTrue();
        pickDropActionGroup.Priority.ShouldBe(Priority.Resume);
        pickDropActionGroup.IsManual.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ForceQueueActionGroup_ValidData_PublishesActionGroupQueued()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false);
        actionGroup.IsQueued.ShouldBeFalse();

        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup);

        await fixture.TestHarness.Bus.Publish(new ForceQueueActionGroup(actionGroup.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<ForceQueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionGroupQueued>();
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
    }

    [TestMethod]
    public async Task TryRetractActionGroup_ValidData_PublishesActionGroupChanged()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(isQueued: true);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithHostOrderRetracted(true)
            .WithMultipleRequiredActionGroups([actionGroup, actionGroup]);

        await fixture.PublishMessageAsync(new TryRetractActionGroup(actionGroup.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        actionGroup.IsQueued.ShouldBeFalse();
    }

    [TestMethod]
    public async Task TryRetractActionGroup_StartedAfterRequest_DoesNotPublishActionGroupChanged()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(isQueued: true);
        var startedActionGroup = new ActionGroup(actionGroup.CorrelationId, actionGroup.OrderId, actionGroup.Actions, actionGroup.Creator) { StartedDate = DateTime.UtcNow };
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithHostOrderRetracted(true)
            .WithMultipleRequiredActionGroups([actionGroup, startedActionGroup]);

        await fixture.PublishMessageAsync(new TryRetractActionGroup(actionGroup.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        actionGroup.IsQueued.ShouldBeTrue();
    }

    [TestMethod]
    public async Task TryRetractActionGroup_NotQueued_DoesNotPublishActionGroupChanged()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(isQueued: false);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithHostOrderRetracted(true)
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new TryRetractActionGroup(actionGroup.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        actionGroup.IsQueued.ShouldBeFalse();
    }

    [TestMethod]
    public async Task TryRetractActionGroup_Started_DoesNotPublishActionGroupChanged()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(isQueued: true, startedDate: DateTime.UtcNow, assignedAgv: Guid.NewGuid());
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithHostOrderRetracted(true)
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new TryRetractActionGroup(actionGroup.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        actionGroup.IsQueued.ShouldBeTrue();
    }

    [TestMethod]
    public async Task TryRetractActionGroup_HostOrderNotRetracted_DoesNotPublishActionGroupChanged()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(isQueued: true);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .WithHostOrderRetracted(false)
            .AddActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new TryRetractActionGroup(actionGroup.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();
        actionGroup.IsQueued.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_HandleActionFailure_ValidData_PublishesActionGroupChanged()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionState.Started);
        var failedAction = actionGroup.Actions.First();
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .WithNodeState(NodeLockGenerator.GenerateNodeLock(isLocked: true));

        var command = new HandleActionFailure(actionGroup.CorrelationId, failedAction.CorrelationId);

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();

        actionGroup.State.ShouldBe(ActionGroupState.Failed);
        failedAction.State.ShouldBe(ActionState.Failed);
    }

    [TestMethod]
    public async Task Consume_HandleActionFailure_UnknownAction_DoesNotPublishActionGroupChanged()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionState.Started);
        ActionGroupTestFixtureBase<ActionCommandHandler> fixture = new ActionCommandHandlerTestFixture()
            .AddActionGroup(actionGroup)
            .WithNodeState(NodeLockGenerator.GenerateNodeLock(isLocked: true));

        var command = new HandleActionFailure(actionGroup.CorrelationId, "unknown".ToGuid());

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldNotHavePublishedAsync<ActionGroupChanged>();

        actionGroup.State.ShouldNotBe(ActionGroupState.Failed);
    }
}