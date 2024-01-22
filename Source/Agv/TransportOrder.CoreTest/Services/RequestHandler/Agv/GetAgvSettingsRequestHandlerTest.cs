using Gebhardt.StoreWare.Agv.Common.Configurations.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvSettingsRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvDataRequest_AgvSettingsNotFound_ExceptionThrown()
    {
        AgvRequestsHandlerTestFixture<GetAgvSettings, GetAgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvSettings, GetAgvSettingsRequestHandler>()
            .WithAgvSettings(null)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NoSettingsFoundException>(async () => await fixture.GetResponseAsync<GotAgvSettings>(new GetAgvSettings(Guid.NewGuid())));
    }
    
    [TestMethod]
    public async Task Consume_GetAgvSettings_ValidData_ReturnsSelectedSettings()
    {
        var correlationId = Guid.NewGuid();
        AgvRequestsHandlerTestFixture<GetAgvsSettings, GetAgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsSettings, GetAgvSettingsRequestHandler>()
            .Build();
        
        Response<GotAgvsSettings> response = await fixture.GetResponseAsync<GotAgvsSettings>(new GetAgvsSettings(new List<Guid> {correlationId}));
    
        var message = response.Message.AgvSettings.Single();
        message.CorrelationId.ShouldBe(correlationId);
        message.BatteryLevelCritical.ShouldBe(fixture.AgvSettings!.BatteryLevelCritical);
        message.BatteryLevelLow.ShouldBe(fixture.AgvSettings.BatteryLevelLow);
        message.MinChargeUp.ShouldBe(fixture.AgvSettings.MinChargeUp);
        message.MaxChargeUp.ShouldBe(fixture.AgvSettings.MaxChargeUp);
    }
}