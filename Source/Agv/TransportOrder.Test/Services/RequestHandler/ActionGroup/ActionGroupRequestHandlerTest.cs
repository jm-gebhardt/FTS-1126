using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            .WithActionGroups(Array.Empty<TransportOrder.Domain.Models.ActionGroup.ActionGroup>())
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
            .WithActionGroups(Array.Empty<TransportOrder.Domain.Models.ActionGroup.ActionGroup>())
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
    public async Task Consume_TriggerActionGroupAbortion_UnknownActionGroup_ThrowsActionGroupNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created);
        ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () => await fixture.GetResponseAsync<ActionGroupAbortionTriggered>(
            new TriggerActionGroupAbortion(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging)));
    }

    [TestMethod]
    public async Task Consume_TriggerActionGroupAbortion_ValidData_ActionGroupAbortionTriggered()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);

        ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture =
            await new ActionGroupRequestHandlerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroup)
                .WithFakeTriggeredAbortAction()
                .BuildAsync();
        Response<ActionGroupAbortionTriggered> response = await fixture.GetResponseAsync<ActionGroupAbortionTriggered>(new TriggerActionGroupAbortion(actionGroup.CorrelationId, AbortReason.WebApi));
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
            new TriggerActionGroupAbortion(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging)));
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
            new TriggerActionGroupAbortion(actionGroup.CorrelationId, AbortReason.ReactivatedFromCharging)));
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
            new TriggerActionGroupAbortion(actionGroupWithStartedAction.CorrelationId, AbortReason.ReactivatedFromCharging)));
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
        Response<ActionGroupResetTriggered> response = await fixture.GetResponseAsync<ActionGroupResetTriggered>(new TriggerActionGroupReset(actionGroup.CorrelationId));
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
            await fixture.GetResponseAsync<ActionGroupResetTriggered>(new TriggerActionGroupReset("Unknown".ToGuid())));
    }
}