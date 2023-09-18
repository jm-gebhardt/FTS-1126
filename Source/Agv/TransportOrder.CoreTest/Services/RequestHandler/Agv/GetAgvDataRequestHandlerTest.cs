using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvDataRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvDataRequest_AgvDataNotFound_ExceptionThrown()
    {
        AgvRequestsHandlerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvDataNotFoundException>(async () => await fixture.GetResponseAsync<GetAgvDataResult>(new GetAgvDataRequest(Guid.NewGuid())));
    }
}