using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

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
    public async Task GetAgvBattery_ValidData_ReturnsBatteryStatus()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler>()
            .WithAgvs([agv])
            .Build();

        Response<GotAgvBattery> response = await fixture.GetResponseAsync<GotAgvBattery>(new GetAgvBattery(agv.CorrelationId));

        var actualData = response.Message.AgvBatteryStatus;
        actualData.CorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvBattery_ValidData_ReturnsSelectedBatteryStatus()
    {
        List<TransportOrder.Domain.Models.Agv.Agv> agvs = AgvGenerator.GenerateIdleAgvs(2).ToList();
        AgvRequestsHandlerTestFixture<GetAgvsBattery, GetAgvBatteryRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsBattery, GetAgvBatteryRequestHandler>()
            .WithAgvs(agvs)
            .Build();

        Response<GotAgvsBattery> response = await fixture.GetResponseAsync<GotAgvsBattery>(new GetAgvsBattery(agvs.Select(a => a.CorrelationId).ToList()));

        List<AgvBatteryStatus> actualData = response.Message.AgvBatteryStatus;
        actualData.Count.ShouldBe(2);
        for (int i = 0; i < 2; i++)
        {
            actualData[i].CorrelationId.ShouldBe(agvs[i].CorrelationId);
            actualData[i].BatteryCharge.ShouldBe(agvs[i].AgvStatus!.BatteryCharge);
            actualData[i].BatteryLevelLow.ShouldBe(fixture.AgvSettings.BatteryLevelLow);
        }
    }
}