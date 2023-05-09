using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvStatusRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvStatusRequest_AgvNotFound_ExceptionThrown()
    {
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvStatusRequestHandler>();

        await fixture.TestHarness!.Bus.Publish(new GetAgvStatusRequest(Guid.Empty));
        (await fixture.TestHarness.Consumed.Any<GetAgvStatusRequest>()).ShouldBeTrue();
        fixture.ShouldContainFault<GetAgvStatusRequest, AgvNotFoundException>();
    }
    
    [TestMethod]
    public async Task GetAgvStatusRequest_AgvStatusNotFound_ExceptionThrown()
    {
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvStatusRequestHandler>();
        fixture.AgvRegistryMock.Setup(r => r.GetAgv(It.IsAny<Guid>())).Returns(Mock.Of<IAgv>());
        
        await fixture.TestHarness!.Bus.Publish(new GetAgvStatusRequest(Guid.Empty));
        (await fixture.TestHarness.Consumed.Any<GetAgvStatusRequest>()).ShouldBeTrue();
        fixture.ShouldContainFault<GetAgvStatusRequest, AgvStatusNotFoundException>();
    }
}