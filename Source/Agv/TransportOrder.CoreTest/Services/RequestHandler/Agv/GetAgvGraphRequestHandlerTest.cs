using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvGraphRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvGraphRequest_AgvNotFound_ExceptionThrown()
    {
        AgvRequestsHandlerTestFixture<GetAgvGraph, GetAgvGraphRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvGraph, GetAgvGraphRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvGraph>(new GetAgvGraph(Guid.NewGuid())));
    }
    
    [TestMethod]
    public async Task GetAgvGraphRequest_GraphNotFound_ExceptionThrown()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        AgvRequestsHandlerTestFixture<GetAgvGraph, GetAgvGraphRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvGraph, GetAgvGraphRequestHandler>()
            .WithEmptyGraphRegistry()
            .WithAgv(agv)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvGraphNotDefinedException>(async () => await fixture.GetResponseAsync<GotAgvGraph>(new GetAgvGraph(agv.CorrelationId)));
    }
}