using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using MassTransit;
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
        AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>()
            .Build();

        Response<GetAgvsResult> response = await fixture.GetResponseAsync<GetAgvsResult>(new GetAgvsRequest());
        response.Message.Agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryContainsAgv_AgvReturned()
    {
        var agvs = new List<IAgv> { Mock.Of<IAgv>() };
        AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>()
            .WithAgvs(agvs)
            .Build();

        Response<GetAgvsResult> response = await fixture.GetResponseAsync<GetAgvsResult>(new GetAgvsRequest());
        response.Message.Agvs.ShouldNotBeEmpty();
    }
}