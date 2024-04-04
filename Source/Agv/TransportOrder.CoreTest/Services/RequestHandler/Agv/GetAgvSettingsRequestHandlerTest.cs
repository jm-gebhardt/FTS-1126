using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvSettingsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetAgvSettings_ValidData_ReturnsSelectedSettings()
    {
        var correlationId = Guid.NewGuid();
        AgvRequestsHandlerTestFixture<GetAgvsSettings, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsSettings, AgvSettingsRequestHandler>()
            .Build();

        Response<GotAgvsSettings> response = await fixture.GetResponseAsync<GotAgvsSettings>(new GetAgvsSettings(new List<Guid> { correlationId }));

        var message = response.Message.AgvSettings.Single();
        message.CorrelationId.ShouldBe(correlationId);
        message.BatteryLevelCritical.ShouldBe(fixture.AgvSettings!.BatteryLevelCritical);
        message.BatteryLevelLow.ShouldBe(fixture.AgvSettings.BatteryLevelLow);
        message.MinChargeUp.ShouldBe(fixture.AgvSettings.MinChargeUp);
        message.MaxChargeUp.ShouldBe(fixture.AgvSettings.MaxChargeUp);
    }

    [TestMethod]
    public async Task Consume_UpdateDefaultSettings_ValidData_SettingsUpdated()
    {
        var group = new AgvSettingsGroup(GroupType.All);
        group.UpdateSettings(EfAgvSettingsProvider.DefaultSettings);
        AgvRequestsHandlerTestFixture<UpdateDefaultAgvSettings, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<UpdateDefaultAgvSettings, AgvSettingsRequestHandler>()
            .WithSettingsGroup(group)
            .Build();

        var settings = new AgvSettingsReadModel(batteryLevelCritical: 10);
        Response<Success> response = await fixture.GetResponseAsync<Success>(new UpdateDefaultAgvSettings(settings));
        response.ShouldNotBeNull();
        fixture.VerifyAgvSettingsUpdated(group.GroupType, group.Identifier, settings);
    }
}