using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

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
        actualData.AgvCorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_GetAgvData_UnknownCorrelationId_CorrelationIdOmitted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetAgvsData, GetAgvDataRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsData, GetAgvDataRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .Build();
        var unknown = "Unknown".ToGuid();
        Response<GotAgvsData> response = await fixture.GetResponseAsync<GotAgvsData>(new GetAgvsData(new List<Guid> { agv.CorrelationId,  unknown}));

        response.Message.AgvData.FirstOrDefault(d => d.AgvCorrelationId == unknown ).ShouldBeNull();
    }
}