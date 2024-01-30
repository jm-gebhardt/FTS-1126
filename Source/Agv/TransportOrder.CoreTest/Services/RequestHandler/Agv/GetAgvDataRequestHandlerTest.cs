using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvDataRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvDataRequest_AgvDataNotFound_ExceptionThrown()
    {
        AgvRequestsHandlerTestFixture<GetAgvData, GetAgvDataRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvData, GetAgvDataRequestHandler>()
            .WithEmptyAgvDataRepository()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvDataNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvData>(new GetAgvData(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task Consume_GetAgvData_ValidData_ReturnsSelectedData()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetAgvsData, GetAgvDataRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsData, GetAgvDataRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .Build();

        Response<GotAgvsData> response = await fixture.GetResponseAsync<GotAgvsData>(new GetAgvsData(new List<Guid> { agv.CorrelationId }));

        var actualData = response.Message.AgvData.Single();
        actualData.CorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_GetAgvData_UnknownCorrelationId_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetAgvsData, GetAgvDataRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsData, GetAgvDataRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvsDataBatchQueryException>(async () => await fixture.GetResponseAsync<GotAgvsData>(new GetAgvsData(new List<Guid> { agv.CorrelationId, "Unknown".ToGuid() })));
    }
}