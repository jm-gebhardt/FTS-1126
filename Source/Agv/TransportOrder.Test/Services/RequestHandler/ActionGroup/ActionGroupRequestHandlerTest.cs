using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.ActionGroup;

[TestClass]
public class ActionGroupRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetActionGroup_ValidCorrelationId_RespondsWithActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created);
        ActionGroupRequestHandlerTestFixture<GetActionGroup, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetActionGroup, ActionGroupRequestHandler>()
            .WithActionGroups(actionGroup)
            .AddDefaultGraph()
            .BuildAsync();

        Response<GotAgvActionGroup> response = await fixture.GetResponseAsync<GotAgvActionGroup>(
            new GetActionGroup(actionGroup.CorrelationId));

        var actualActionGroup = response.Message.AgvActionGroup;
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actualActionGroup.Actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task Consume_GetActionGroup_UnknownCorrelationId_ThrowsActionGroupNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created);
        ActionGroupRequestHandlerTestFixture<GetActionGroup, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetActionGroup, ActionGroupRequestHandler>()
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvActionGroup>(
            new GetActionGroup(actionGroup.CorrelationId)));
    }

    [TestMethod]
    public async Task Consume_GetAgvActionGroupsRequest_EmptyActionGroupRepository_ReturnsEmptyList()
    {
        ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler>()
            .WithActionGroups()
            .BuildAsync();
        Response<GotActionGroups> response = await fixture.GetResponseAsync<GotActionGroups>(
            new GetActionGroups(fixture.AllActionGroupStates, DateTime.MinValue, DateTime.MaxValue, null, 100));
        response.Message.ActionGroups.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_GetAgvActionGroupsRequest_SingleActionGroup_ReturnsCreated()
    {
        TransportOrder.Domain.Models.ActionGroup.ActionGroup[] actionGroups = { AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created) };
        ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler>()
            .WithActionGroups(actionGroups)
            .AddDefaultGraph()
            .BuildAsync();

        Response<GotActionGroups> response = await fixture.GetResponseAsync<GotActionGroups>(
            new GetActionGroups(fixture.AllActionGroupStates, DateTime.MinValue, DateTime.MaxValue, null, 100));
        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModels = response.Message.ActionGroups;
        actionGroupReadModels.ShouldNotBeEmpty();
        actionGroups.ShouldAllBe(g => g.Actions.Any());
    }

    [TestMethod]
    public async Task Consume_GetAgvActionGroupsRequest_MultipleActionGroups_ReturnsOnlyAssignedOnes()
    {
        var agvCorrelationId = Guid.NewGuid();
        TransportOrder.Domain.Models.ActionGroup.ActionGroup[] actionGroups =
        {
            AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created),
            AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Canceled),
            AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created, assignedAgv: agvCorrelationId),
            AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started, assignedAgv: agvCorrelationId),
            AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Failed),
            AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Invalid)
        };
        ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler>()
            .WithActionGroups(actionGroups)
            .AddDefaultGraph()
            .BuildAsync();

        Response<GotActionGroups> response = await fixture.GetResponseAsync<GotActionGroups>(
            new GetActionGroups(fixture.AllActionGroupStates, DateTime.MinValue, DateTime.MaxValue, new List<Guid>() { agvCorrelationId }, 100));
        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModels = response.Message.ActionGroups;
        actionGroupReadModels.Count.ShouldBe(2);
        actionGroupReadModels.ShouldContain(a => a.State == ActionGroupState.Created);
        actionGroupReadModels.ShouldContain(a => a.State == ActionGroupState.Started);
    }

    private static IEnumerable<TransportOrder.Domain.Models.ActionGroup.ActionGroup> CreateInitialActionGroups(int totalActionGroup, ActionGroupState actionGroupState)
    {
        var actionGroups = new List<TransportOrder.Domain.Models.ActionGroup.ActionGroup>();
        for (int i = 0; i < totalActionGroup; i++)
        {
            var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
            actionGroups.Add(AgvActionGenerator.SetActionGroupToState(actionGroup, actionGroupState));
        }
        return actionGroups;
    }

    [TestMethod]
    public async Task GetAgvActionGroupsRequestHandler_EmptyActionGroupRepository_ReturnsEmptyActionGroupsCountByState()
    {
        ActionGroupRequestHandlerTestFixture<GetActionGroupCountsByState, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetActionGroupCountsByState, ActionGroupRequestHandler>()
            .WithActionGroups()
            .BuildAsync();

        Response<GotActionGroupCountsByState> response = await fixture.GetResponseAsync<GotActionGroupCountsByState>(
            new GetActionGroupCountsByState());

        IEnumerable<AgvGroupCountByState> actionGroupsCount = response.Message.AgvGroupCountByState;
        actionGroupsCount.ShouldAllBe(x => x.Count == 0);
    }

    [TestMethod]
    [DataRow(1, 1, 1)]
    [DataRow(0, 2, 3)]
    [DataRow(1, 0, 3)]
    [DataRow(1, 2, 0)]
    [DataRow(0, 0, 0)]
    public async Task GetAgvActionGroupsRequestHandler_WhenThereAreActionGroups_ReturnsActionGroupsCountByState(int actionGroupCreatedCount, int actionGroupStartedCount, int actionGroupCompletedCount)
    {
        var actionGroups = new List<TransportOrder.Domain.Models.ActionGroup.ActionGroup>();
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupCreatedCount, ActionGroupState.Created));
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupStartedCount, ActionGroupState.Started));
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupCompletedCount, ActionGroupState.Completed));
        ActionGroupRequestHandlerTestFixture<GetActionGroupCountsByState, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetActionGroupCountsByState, ActionGroupRequestHandler>()
            .WithActionGroups(actionGroups.ToArray())
            .BuildAsync();

        Response<GotActionGroupCountsByState> response = await fixture.GetResponseAsync<GotActionGroupCountsByState>(
            new GetActionGroupCountsByState());

        IEnumerable<AgvGroupCountByState> actionGroupsCount = response.Message.AgvGroupCountByState;
        foreach (var actionGroupsByState in actionGroupsCount)
        {
            switch (actionGroupsByState)
            {
                case { State: ActionGroupState.Completed }:
                    actionGroupsByState.Count.ShouldBe(actionGroupCompletedCount);
                    break;

                case { State: ActionGroupState.Created }:
                    actionGroupsByState.Count.ShouldBe(actionGroupCreatedCount);
                    break;

                case { State: ActionGroupState.Started }:
                    actionGroupsByState.Count.ShouldBe(actionGroupStartedCount);
                    break;
            }
        }
    }

    [TestMethod]
    [DataRow("906", new[] { "906000000136", "906000000999" })]
    [DataRow("136", new[] { "906000000136" })]
    [DataRow("999", new[] { "906000000999" })]
    [DataRow("000", new[] { "906000000136", "906000000999" })]
    [DataRow("XYZ", new string[0])]
    public async Task Consume_GetHandlingUnits_VariousQueries_ReturnsExpected(string query, string[] expected)
    {
        string[] handlingUnits = ["906000000136", "906000000999"];

        ActionGroupRequestHandlerTestFixture<GetHandlingUnits, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetHandlingUnits, ActionGroupRequestHandler>()
            .WithActionGroups(AgvActionGenerator.GenerateByHandlingUnits(handlingUnits).ToArray())
            .BuildAsync();

        Response<GotHandlingUnits> response = await fixture.GetResponseAsync<GotHandlingUnits>(new GetHandlingUnits(query));
        response.Message.HandlingUnits.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Consume_GetHandlingUnits_ResultsAreOrderedAscending()
    {
        string[] handlingUnits = ["ZZZ0001", "AAA0002", "MMM0003"];

        ActionGroupRequestHandlerTestFixture<GetHandlingUnits, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetHandlingUnits, ActionGroupRequestHandler>()
            .WithActionGroups(AgvActionGenerator.GenerateByHandlingUnits(handlingUnits).ToArray())
            .BuildAsync();

        Response<GotHandlingUnits> response = await fixture.GetResponseAsync<GotHandlingUnits>(new GetHandlingUnits("000", 10));

        response.Message.HandlingUnits.ShouldBe(["AAA0002", "MMM0003", "ZZZ0001"]);
    }

    [TestMethod]
    public async Task Consume_GetHandlingUnits_LimitZero_DefaultsToOne()
    {
        string[] handlingUnits = ["HU(123)", "HU(1234)"];

        ActionGroupRequestHandlerTestFixture<GetHandlingUnits, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetHandlingUnits, ActionGroupRequestHandler>()
            .WithActionGroups(AgvActionGenerator.GenerateByHandlingUnits(handlingUnits).ToArray())
            .BuildAsync();

        Response<GotHandlingUnits> response = await fixture.GetResponseAsync<GotHandlingUnits>(new GetHandlingUnits("123", 0));

        response.Message.HandlingUnits.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_GetHandlingUnits_LimitAbove100_IsClampedTo100()
    {
        string[] handlingUnits = Enumerable.Range(0, 200).Select(i => $"HU({i})").ToArray();

        ActionGroupRequestHandlerTestFixture<GetHandlingUnits, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetHandlingUnits, ActionGroupRequestHandler>()
            .WithActionGroups(AgvActionGenerator.GenerateByHandlingUnits(handlingUnits).ToArray())
            .BuildAsync();

        Response<GotHandlingUnits> response = await fixture.GetResponseAsync<GotHandlingUnits>(new GetHandlingUnits("HU(", 9999));

        response.Message.HandlingUnits.Count.ShouldBe(100);
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupAbortion_UnknownActionGroup_ThrowsActionGroupNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created);
        ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () => await fixture.GetResponseAsync<ActionGroupAbortionTriggered>(
            new TriggerActionGroupAbortion(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging, "user")));
    }

    [DataRow(ActionState.Paused)]
    [DataRow(ActionState.Started)]
    [TestMethod]
    public async Task Consume_TriggerActionGroupAbortion_ValidData_ActionGroupAbortionTriggered(ActionState actionState)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: actionState);

        ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .WithFakeTriggeredAbortAction()
                .BuildAsync();
        Response<ActionGroupAbortionTriggered> response = await fixture.GetResponseAsync<ActionGroupAbortionTriggered>(new TriggerActionGroupAbortion(actionGroup.CorrelationId, AbortReason.WebApi, "user"));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldHavePublishedAsync<AbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupAbortion_ActionGroupInWrongState_ThrowsActionGroupStateInvalidException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created);
        ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
            .WithActionGroups(actionGroup)
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupStateInvalidException>(async () => await fixture.GetResponseAsync<ActionGroupAbortionTriggered>(
            new TriggerActionGroupAbortion(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging, "user")));
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupAbortion_NoAssignedAgv_ThrowsActionGroupStateInvalidException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null, pickActionState: ActionState.Started);

        ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .WithFakeFailedAbortAction()
                .BuildAsync();
        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupStateInvalidException>(async () => await fixture.GetResponseAsync<ActionGroupAbortionTriggered>(
            new TriggerActionGroupAbortion(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging, "user")));
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupAbortion_ActionsCompletedInTheMeantime_ThrowsActionGroupStateInvalidException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroupWithStartedAction = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);

        ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroupWithStartedAction)
                .WithFakeFailedAbortAction()
                .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupStateInvalidException>(async () => await fixture.GetResponseAsync<ActionGroupAbortionTriggered>(
            new TriggerActionGroupAbortion(actionGroupWithStartedAction.CorrelationId, AbortReason.ReactivatedFromCharging, "user")));
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupReset_ValidData_ActionGroupResetTriggered()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);

        ActionGroupRequestHandlerTestFixture<TriggerActionGroupReset, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupReset, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .WithFakeTriggeredResetAction()
                .BuildAsync();
        Response<ActionGroupResetTriggered> response = await fixture.GetResponseAsync<ActionGroupResetTriggered>(new TriggerActionGroupReset(actionGroup.CorrelationId, false, "user"));
        response.Message.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        await fixture.ShouldHavePublishedAsync<ResetActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupReset_UnknownActionGroup_ThrowsActionGroupNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);

        ActionGroupRequestHandlerTestFixture<TriggerActionGroupReset, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupReset, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .BuildAsync();
        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () =>
            await fixture.GetResponseAsync<ActionGroupResetTriggered>(new TriggerActionGroupReset("Unknown".ToGuid(), false, "user")));
    }

    [TestMethod]
    public async Task Consume_TryReassignActionGroup_UnknownActionGroup_ThrowsActionGroupNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: ActionState.Started);

        ActionGroupRequestHandlerTestFixture<TryReassignActionGroup, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TryReassignActionGroup, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup).BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryReassignActionGroup("Unknown".ToGuid(), null, "user")));
    }

    [DataRow(ActionState.Created)]
    [DataRow(ActionState.Aborted)]
    [DataRow(ActionState.Failed)]
    [DataRow(ActionState.Canceled)]
    [DataRow(ActionState.Completed)]
    [DataRow(ActionState.Invalid)]
    [TestMethod]
    public async Task Consume_TryReassignActionGroup_PickActionNotStarted_ThrowsActionGroupStateInvalidException(ActionState pickActionState)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: pickActionState);

        ActionGroupRequestHandlerTestFixture<TryReassignActionGroup, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TryReassignActionGroup, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup).BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupStateInvalidException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryReassignActionGroup(actionGroup.CorrelationId, null, "user")));
    }

    [DataRow(ActionState.Started, false)]
    [DataRow(ActionState.Paused, false)]
    [DataRow(ActionState.Started, true)]
    [DataRow(ActionState.Paused, true)]
    [TestMethod]
    public async Task Consume_TryReassignActionGroup_ValidData_PublishesReassignActionGroup(ActionState pickActionState, bool assignedAgv)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: pickActionState);
        ActionGroupRequestHandlerTestFixture<TryReassignActionGroup, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TryReassignActionGroup, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup).BuildAsync();

        await fixture.GetResponseAsync<Success>(new TryReassignActionGroup(actionGroup.CorrelationId, assignedAgv ? Guid.NewGuid() : null, "user"));

        await fixture.ShouldHavePublishedAsync<ReassignActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupQueueing_ValidData_PublishesForceQueueActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupRequestHandlerTestFixture<TriggerActionGroupQueueing, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupQueueing, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup).BuildAsync();

        await fixture.GetResponseAsync<Success>(new TriggerActionGroupQueueing(actionGroup.CorrelationId, "user"));

        await fixture.ShouldHavePublishedAsync<ForceQueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerSetTarget_ValidData_RespondsSuccess()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(pickActionState: ActionState.Created);
        ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler>()
            .WithActionGroups(actionGroup)
            .AddDefaultGraph()
            .BuildAsync();

        var node = fixture.GetExistingNode(AgvGraph.NodeType.Drop);
        await fixture.GetResponseAsync<Success>(new TriggerSetTarget(actionGroup.CorrelationId, actionGroup.Actions.First(a => a.IsIncomplete).CorrelationId, node, "user"));

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
    }

    [TestMethod]
    public async Task Consume_TriggerSetTarget_UnknownActionGroup_ThrowsActionGroupNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(pickActionState: ActionState.Created);

        ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .AddDefaultGraph()
                .BuildAsync();

        var node = fixture.GetExistingNode();
        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TriggerSetTarget("unknown".ToGuid(), actionGroup.Actions.First(a => a.IsIncomplete).CorrelationId, node, "user")));
    }

    [TestMethod]
    public async Task Consume_TriggerSetTarget_UnknownAction_ThrowsActionNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(pickActionState: ActionState.Created);

        ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .AddDefaultGraph()
                .BuildAsync();

        var node = fixture.GetExistingNode();
        await MediatorShould.ThrowRequestExceptionAsync<ActionNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TriggerSetTarget(actionGroup.CorrelationId, "unknown".ToGuid(), node, "user")));
    }

    [TestMethod]
    public async Task Consume_TriggerSetTarget_UnknownTargetNode_ThrowsNodeNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(pickActionState: ActionState.Created);

        ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .AddDefaultGraph()
                .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TriggerSetTarget(actionGroup.CorrelationId, actionGroup.Actions.First(a => a.IsIncomplete).CorrelationId, "unknown".ToGuid(), "user")));
    }

    [TestMethod]
    public async Task Consume_TriggerSetTarget_ActionIsAlreadyCompleted_ThrowsActionGroupStateInvalidException()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(dropActionState: ActionState.Completed);

        ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .AddDefaultGraph()
                .BuildAsync();

        var node = fixture.GetExistingNode();
        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupStateInvalidException>(async () =>
            await fixture.GetResponseAsync<Success>(new TriggerSetTarget(actionGroup.CorrelationId, actionGroup.Actions.First(a => a.IsIncomplete).CorrelationId, node, "user")));
    }

    [TestMethod]
    public async Task Consume_TriggerSetTarget_ActionShouldSkip_ThrowsActionGroupStateInvalidException()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(pickActionState: ActionState.Completed);
        var actionToUpdate = actionGroup.Actions.First(a => a.IsIncomplete);
        var targetNode = "target".ToGraphNode();
        actionGroup.SetTargetForActionWithUpdate(actionToUpdate.CorrelationId, targetNode);
        ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .AddDefaultGraph()
                .BuildAsync();

        var node = fixture.GetExistingNode();
        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupStateInvalidException>(async () =>
            await fixture.GetResponseAsync<Success>(new TriggerSetTarget(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.ShouldSkip).CorrelationId, node, "user")));
    }

    public static IEnumerable<object[]> AllNodeTypes() => Enum.GetValues(typeof(AgvGraph.NodeType)).Cast<AgvGraph.NodeType>().Select(v => new object[] { v });

    [TestMethod]
    [DynamicData(nameof(AllNodeTypes))]
    public async Task Consume_TriggerSetTarget_ForPickActionWrongTargetNodeType_ThrowsInvalidNodeTypeException(AgvGraph.NodeType nodeType)
    {
        if (nodeType is AgvGraph.NodeType.Pick or AgvGraph.NodeType.PickDrop)
        {
            return;
        }
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(pickActionState: ActionState.Created);

        ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .AddDefaultGraph(Guid.NewGuid(), AllNodeTypes().Select(n =>
                {
                    var type = (AgvGraph.NodeType) n[0];
                    return NodeGenerator.GenerateNode(nodeName: type.ToString(), nodeType: type);
                }).ToList())
                .BuildAsync();

        var node = fixture.GetExistingNode(nodeType);
        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () =>
            await fixture.GetResponseAsync<Success>(new TriggerSetTarget(actionGroup.CorrelationId, actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).CorrelationId, node, "user")));
    }

    [TestMethod]
    [DynamicData(nameof(AllNodeTypes))]
    public async Task Consume_TriggerSetTarget_ForDropActionWrongTargetNodeType_ThrowsInvalidNodeTypeException(AgvGraph.NodeType nodeType)
    {
        if (nodeType is AgvGraph.NodeType.Drop or AgvGraph.NodeType.PickDrop)
        {
            return;
        }
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(pickActionState: ActionState.Created);

        ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerSetTarget, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .AddDefaultGraph(Guid.NewGuid(), AllNodeTypes().Select(n =>
                {
                    var type = (AgvGraph.NodeType) n[0];
                    return NodeGenerator.GenerateNode(nodeName: type.ToString(), nodeType: type);
                }).ToList())
                .BuildAsync();

        var node = fixture.GetExistingNode(nodeType);
        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () =>
            await fixture.GetResponseAsync<Success>(new TriggerSetTarget(actionGroup.CorrelationId, actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).CorrelationId, node, "user")));
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupCompletion_ValidData_PublishesCompleteActionOfActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);

        ActionGroupRequestHandlerTestFixture<TriggerActionCompletion, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerActionCompletion, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .WithFakeTriggeredActionCompletion()
                .BuildAsync();

        await fixture.GetResponseAsync<Success>(new TriggerActionCompletion(actionGroup.CorrelationId, actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).CorrelationId, "user"));

        await fixture.ShouldHavePublishedAsync<CompleteActionOfActionGroup>();
        await fixture.ShouldHavePublishedAsync<ActionAborted>();
    }
}