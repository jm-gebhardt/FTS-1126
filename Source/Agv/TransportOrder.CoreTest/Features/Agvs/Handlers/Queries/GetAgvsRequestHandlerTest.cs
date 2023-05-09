using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvsRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryEmpty_ReturnsEmptyList()
    {
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequestHandler>();

        await fixture.TestHarness!.Bus.Publish(new GetAgvsRequest());
        (await fixture.TestHarness.Consumed.Any<GetAgvsRequest>()).ShouldBeTrue();
        await fixture.TestHarness.Published.Any<GetAgvsResult>();
        
        IPublishedMessage<GetAgvsResult>? result = fixture.TestHarness.Published.Select<GetAgvsResult>().First();
        result.Context.Message.Agvs.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryContainsAgv_AgvReturned()
    {
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequestHandler>();
        var agvs = new List<IAgv> { Mock.Of<IAgv>() }; 
        fixture.AgvRegistryMock.Setup(s => s.GetAllAgvs())
            .Returns(agvs.AsQueryable());
        await fixture.TestHarness!.Bus.Publish(new GetAgvsRequest());
        (await fixture.TestHarness.Consumed.Any<GetAgvsRequest>()).ShouldBeTrue();
        await fixture.TestHarness.Published.Any<GetAgvsResult>();
        
        IPublishedMessage<GetAgvsResult>? result = fixture.TestHarness.Published.Select<GetAgvsResult>().First();
        result.Context.Message.Agvs.ShouldNotBeEmpty();
    }
    
}