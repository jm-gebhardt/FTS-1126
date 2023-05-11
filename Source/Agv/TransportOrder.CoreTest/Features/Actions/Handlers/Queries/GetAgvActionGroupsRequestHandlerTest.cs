using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Actions.Handlers.Queries;

[TestClass]
public class GetAgvActionGroupsRequestHandlerTest
{
    [TestMethod]
    public async Task EmptyActionRegistry_ReturnsEmptyList()
    {
        var fixture = new ActionRequestHandlerTestFixture<GetAgvActionGroupsRequestHandler>();
        fixture.ActionRegistryMock
            .Setup(x => x.GetActionGroups(fixture.AllActionGroupStates))
            .Returns<ActionGroupState[]>(x => Array.Empty<ActionGroup>().AsQueryable());
        
        await fixture.TestHarness!.Bus.Publish(new GetAgvActionGroupsRequest(
            fixture.AllActionGroupStates,
            DateTime.MinValue, 
            DateTime.MaxValue, 
            100));
        
        (await fixture.TestHarness.Consumed.Any<GetAgvActionGroupsRequest>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<GetAgvActionGroupsResult>()).ShouldBeTrue();
        
        IPublishedMessage<GetAgvActionGroupsResult>? result = fixture.TestHarness.Published.Select<GetAgvActionGroupsResult>().First();
        result.Context.Message.AgvActionGroups.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task SingleActionGroup_ReturnsCreated()
    {
        var fixture = new ActionRequestHandlerTestFixture<GetAgvActionGroupsRequestHandler>();
        ActionGroup[] actions = { fixture.CreatePickDropActionGroup(ActionState.Created) };
        
        fixture.ActionRegistryMock
            .Setup(x => x.GetActionGroups(fixture.AllActionGroupStates))
            .Returns<ActionGroupState[]>(x => actions.AsQueryable());

        fixture.ActionRegistryMock.Setup(x => x.GetActionGroups(It.IsAny<ActionGroupState[]>()));
        await fixture.TestHarness!.Bus.Publish(new GetAgvActionGroupsRequest(
            fixture.AllActionGroupStates,
            DateTime.MinValue,
            DateTime.MaxValue,
            100));
        
        (await fixture.TestHarness.Consumed.Any<GetAgvActionGroupsRequest>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<GetAgvActionGroupsResult>()).ShouldBeTrue();
        
        IPublishedMessage<GetAgvActionGroupsResult>? result = fixture.TestHarness.Published.Select<GetAgvActionGroupsResult>().First();
        result.Context.Message.AgvActionGroups.Count().ShouldBe(1);
        result.Context.Message.AgvActionGroups.ShouldContain(x => x.CorrelationId == actions[0].CorrelationId);
    }
}