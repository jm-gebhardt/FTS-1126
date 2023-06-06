using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvGraphRequestHandlerTest
{
    /*
    [TestMethod]
    public async Task GetAgvGraphRequest_AgvNotFound_ExceptionThrown()
    {
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvGraphRequestHandler>();

        await fixture.TestHarness!.Bus.Publish(new GetAgvGraphRequest(Guid.Empty));
        (await fixture.TestHarness.Consumed.Any<GetAgvGraphRequest>()).ShouldBeTrue();
        fixture.ShouldContainFault<GetAgvGraphRequest, AgvNotFoundException>();
    }
    
    [TestMethod]
    public async Task GetAgvGraphRequest_GraphNotFound_ExceptionThrown()
    {
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvGraphRequestHandler>();
        fixture.AgvRegistryMock.Setup(r => r.GetAgv(It.IsAny<Guid>())).Returns(Mock.Of<IAgv>());
        await fixture.TestHarness!.Bus.Publish(new GetAgvGraphRequest(Guid.Empty));
        (await fixture.TestHarness.Consumed.Any<GetAgvGraphRequest>()).ShouldBeTrue();
        fixture.ShouldContainFault<GetAgvGraphRequest, AgvGraphNotDefinedException>();
    }*/
}