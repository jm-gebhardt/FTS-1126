using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.CommonTest.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvGraphRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvGraphRequest_AgvNotFound_ExceptionThrown()
    {
        AgvRequestsHandlerTestFixture<GetAgvGraphRequest, GetAgvGraphRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvGraphRequest, GetAgvGraphRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GetAgvGraphResult>(new GetAgvGraphRequest(Guid.NewGuid())));
    }
    
    [TestMethod]
    public async Task GetAgvGraphRequest_GraphNotFound_ExceptionThrown()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        AgvRequestsHandlerTestFixture<GetAgvGraphRequest, GetAgvGraphRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvGraphRequest, GetAgvGraphRequestHandler>()
            .WithAgv(agv)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvGraphNotDefinedException>(async () => await fixture.GetResponseAsync<GetAgvGraphResult>(new GetAgvGraphRequest(agv.CorrelationId)));
    }
}