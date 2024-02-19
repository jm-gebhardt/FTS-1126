using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvBatteryRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvBatteryRequest_AgvNotFound_ExceptionThrown()
    {
        AgvRequestsHandlerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler>()
            .WithEmptyAgvRepository()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvBattery>(new GetAgvBattery(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task GetAgvBattery_ValidData_ReturnsSelectedData()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler>()
            .WithAgvs([agv])
            .Build();

        Response<GotAgvBattery> response = await fixture.GetResponseAsync<GotAgvBattery>(new GetAgvBattery(agv.CorrelationId));

        var actualData = response.Message.AgvBatteryStatus;
        actualData.CorrelationId.ShouldBe(agv.CorrelationId);
    }
}