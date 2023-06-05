using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.CommonTest.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Actions.Handlers.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvActionGroupsCountByStateHandlerTest
{
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
        ActionRequestHandlerTestFixture<GetAgvActionGroupCountsByStateRequest, GetAgvActionGroupCountsByStateRequestHandler> fixture = await new ActionRequestHandlerTestFixture<GetAgvActionGroupCountsByStateRequest, GetAgvActionGroupCountsByStateRequestHandler>()
            .WithActionGroups(Array.Empty<ActionGroup>())
            .BuildAsync();
        
        Response<GetAgvActionGroupCountsByStateResult> response = await fixture.GetResponseAsync<GetAgvActionGroupCountsByStateResult>(
            new GetAgvActionGroupCountsByStateRequest());
        
        IEnumerable<AgvGroupCountByStateDto> actionGroupsCount = response.Message.AgvGroupCountByState;
        actionGroupsCount.ShouldAllBe(x => x.Count == 0);
    }
    
    [TestMethod]
    [DataRow(1,1,1)]
    [DataRow(0,2,3)]
    [DataRow(1,0,3)]
    [DataRow(1,2,0)]
    [DataRow(0,0,0)]
    public async Task GetAgvActionGroupsRequestHandler_WhenThereAreActionGroups_ReturnsActionGroupsCountByState(int actionGroupCreatedCount, int actionGroupStartedCount, int actionGroupCompletedCount)
    {
        var actionGroups = new List<ActionGroup>();
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupCreatedCount, ActionGroupState.Created));
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupStartedCount, ActionGroupState.Started));
        actionGroups.AddRange(CreateInitialActionGroups(actionGroupCompletedCount, ActionGroupState.Completed));
        ActionRequestHandlerTestFixture<GetAgvActionGroupCountsByStateRequest, GetAgvActionGroupCountsByStateRequestHandler> fixture = await new ActionRequestHandlerTestFixture<GetAgvActionGroupCountsByStateRequest, GetAgvActionGroupCountsByStateRequestHandler>()
            .WithActionGroups(actionGroups)
            .BuildAsync();
        
        Response<GetAgvActionGroupCountsByStateResult> response = await fixture.GetResponseAsync<GetAgvActionGroupCountsByStateResult>(
            new GetAgvActionGroupCountsByStateRequest());
        
        IEnumerable<AgvGroupCountByStateDto> actionGroupsCount = response.Message.AgvGroupCountByState;
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