using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvDataRequestHandlerTest
{
    /*[TestMethod]
    public async Task GetAgvDataRequest_AgvDataNotFound_ExceptionThrown()
    {
        AgvRequestsHandlerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvDataNotFoundException>(async () => await fixture.GetResponseAsync<GetAgvDataResult>(new GetAgvDataRequest(Guid.NewGuid())));
    }
}