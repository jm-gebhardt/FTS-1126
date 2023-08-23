using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Actions.Handlers.Queries;

[TestClass]
public class ActionGroupRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetActionGroup_ValidCorrelationId_RespondsWithActionGroup()
    {

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(actionState: ActionState.Created);
        ActionGroupRequestHandlerTestFixture<GetActionGroup, ActionGroupRequestHandler> fixture = await (await new ActionGroupRequestHandlerTestFixture<GetActionGroup, ActionGroupRequestHandler>()
                .WithActionGroups(new[]{actionGroup}))
            .BuildAsync();
        await fixture.AddDefaultGraph("aGraph".ToGuid());
        
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

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(actionState: ActionState.Created);
        ActionGroupRequestHandlerTestFixture<GetActionGroup, ActionGroupRequestHandler> fixture = await new ActionGroupRequestHandlerTestFixture<GetActionGroup, ActionGroupRequestHandler>()
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvActionGroup>(
            new GetActionGroup(actionGroup.CorrelationId)));
    }

    [TestMethod]
    public async Task Consume_GetAgvActionGroupsRequest_EmptyActionGroupRegistry_ReturnsEmptyList()
    {
        ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler> fixture = await (await new ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler>()
            .WithActionGroups(Array.Empty<ActionGroup>())).BuildAsync();
        Response<GotActionGroups> response = await fixture.GetResponseAsync<GotActionGroups>(
            new GetActionGroups(fixture.AllActionGroupStates, DateTime.MinValue, DateTime.MaxValue, 100));
        response.Message.ActionGroups.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_GetAgvActionGroupsRequest_SingleActionGroup_ReturnsCreated()
    {
        ActionGroup[] actionGroups = { AgvActionGenerator.GeneratePickDropActionGroup(actionState: ActionState.Created) };
        ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler> fixture = await (await new ActionGroupRequestHandlerTestFixture<GetActionGroups, ActionGroupRequestHandler>()
                .WithActionGroups(actionGroups))
            .BuildAsync();

        Response<GotActionGroups> response = await fixture.GetResponseAsync<GotActionGroups>(
            new GetActionGroups(fixture.AllActionGroupStates, DateTime.MinValue, DateTime.MaxValue, 100));
        response.Message.ActionGroups.ShouldNotBeEmpty();
    }

    private static IEnumerable<ActionGroup> CreateInitialActionGroups(int totalActionGroup, ActionGroupState actionGroupState)
    {
        var actionGroups = new List<ActionGroup>();
        for (int i = 0; i < totalActionGroup; i++)
        {
            actionGroups.Add(AgvActionGenerator.GeneratePickDropActionGroup(actionGroupState));
        }
        return actionGroups;
    }

    [TestMethod]
    public async Task GetAgvActionGroupsRequestHandler_EmptyActionGroupRegistry_ReturnsEmptyActionGroupsCountByState()
    {
        ActionGroupRequestHandlerTestFixture<GetActionGroupCountsByState, ActionGroupRequestHandler> fixture = await (await new ActionGroupRequestHandlerTestFixture<GetActionGroupCountsByState, ActionGroupRequestHandler>()
            .WithActionGroups(Array.Empty<ActionGroup>()))
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
        var actionGroups = new List<ActionGroup>();
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupCreatedCount, ActionGroupState.Created));
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupStartedCount, ActionGroupState.Started));
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupCompletedCount, ActionGroupState.Completed));
        ActionGroupRequestHandlerTestFixture<GetActionGroupCountsByState, ActionGroupRequestHandler> fixture = await (await new ActionGroupRequestHandlerTestFixture<GetActionGroupCountsByState, ActionGroupRequestHandler>()
            .WithActionGroups(actionGroups.ToArray()))
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
}