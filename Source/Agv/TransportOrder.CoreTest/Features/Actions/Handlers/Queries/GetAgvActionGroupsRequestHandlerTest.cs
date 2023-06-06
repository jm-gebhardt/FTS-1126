using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Actions.Handlers.Queries;

[TestClass]
public class GetAgvActionGroupsRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvActionGroupsRequestHandler_EmptyActionGroupRegistry_ReturnsEmptyList()
    {
        ActionRequestHandlerTestFixture<GetAgvActionGroupsRequest, GetAgvActionGroupsRequestHandler> fixture = await new ActionRequestHandlerTestFixture<GetAgvActionGroupsRequest, GetAgvActionGroupsRequestHandler>()
            .WithActionGroups(Array.Empty<ActionGroup>())
            .BuildAsync();
        
        Response<GetAgvActionGroupsResult> response = await fixture.GetResponseAsync<GetAgvActionGroupsResult>(
            new GetAgvActionGroupsRequest(fixture.AllActionGroupStates, DateTime.MinValue, DateTime.MaxValue, 100));
        response.Message.AgvActionGroups.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task GetAgvActionGroupsRequestHandler_SingleActionGroup_ReturnsCreated()
    {
        ActionGroup[] actionGroups = { AgvActionGenerator.GeneratePickDropActionGroup(actionState: ActionState.Created)};
        ActionRequestHandlerTestFixture<GetAgvActionGroupsRequest, GetAgvActionGroupsRequestHandler> fixture = await new ActionRequestHandlerTestFixture<GetAgvActionGroupsRequest, GetAgvActionGroupsRequestHandler>()
            .WithActionGroups(actionGroups)
            .BuildAsync();
        
        Response<GetAgvActionGroupsResult> response = await fixture.GetResponseAsync<GetAgvActionGroupsResult>(
            new GetAgvActionGroupsRequest(fixture.AllActionGroupStates, DateTime.MinValue, DateTime.MaxValue, 100));
        response.Message.AgvActionGroups.ShouldNotBeEmpty();
    }
}