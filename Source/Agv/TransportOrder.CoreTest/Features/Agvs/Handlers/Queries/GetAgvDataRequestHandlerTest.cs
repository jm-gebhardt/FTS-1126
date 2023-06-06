using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvDataRequestHandlerTest
{
    /*[TestMethod]
    public async Task GetAgvDataRequest_AgvDataNotFound_ExceptionThrown()
    {
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvDataRequestHandler>();
        fixture.AgvRegistryMock.Setup(r => r.GetAgv(It.IsAny<Guid>())).Returns(Mock.Of<IAgv>());

        await fixture.TestHarness!.Bus.Publish(new GetAgvDataRequest(Guid.Empty));
        (await fixture.TestHarness.Consumed.Any<GetAgvDataRequest>()).ShouldBeTrue();
        
        fixture.ShouldContainFault<GetAgvDataRequest, AgvDataNotFoundException>();
    }*/
}