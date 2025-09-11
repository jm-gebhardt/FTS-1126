using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class AgvSettingsRequestHandlerTest
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
        message.BatteryLevelCritical.ShouldBe(fixture.AgvSettings.BatteryLevelCritical);
        message.BatteryLevelLow.ShouldBe(fixture.AgvSettings.BatteryLevelLow);
        message.MinChargeUp.ShouldBe(fixture.AgvSettings.MinChargeUp);
        message.MaxChargeUp.ShouldBe(fixture.AgvSettings.MaxChargeUp);
    }

    [TestMethod]
    public async Task Consume_UpdateAgvSettings_ValidData_ReturnsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<UpdateAgvSettings, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<UpdateAgvSettings, AgvSettingsRequestHandler>()
            .WithAgv(agv)
            .Build();

        (await fixture.GetResponseAsync<Success>(new UpdateAgvSettings(agv.CorrelationId, EfAgvSettingsProvider.DefaultSettings, "user"))).ShouldNotBeNull();
        await fixture.ShouldHavePublishedAsync<UpdateAgvSettingsGroupReference>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvSettings_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<UpdateAgvSettings, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<UpdateAgvSettings, AgvSettingsRequestHandler>()
            .WithAgv(agv)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<Success>(new UpdateAgvSettings("unknown".ToGuid(), EfAgvSettingsProvider.DefaultSettings, "user")));
        await fixture.ShouldNotHavePublishedAsync<UpdateAgvSettingsGroupReference>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvsSettings_ValidData_ReturnsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<UpdateAgvsSettings, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<UpdateAgvsSettings, AgvSettingsRequestHandler>()
            .WithAgv(agv)
            .Build();

        (await fixture.GetResponseAsync<Success>(new UpdateAgvsSettings([agv.CorrelationId], EfAgvSettingsProvider.DefaultSettings, "user"))).ShouldNotBeNull();
        await fixture.ShouldHavePublishedAsync<UpdateAgvSettingsGroupReference>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvsSettings_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<UpdateAgvsSettings, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<UpdateAgvsSettings, AgvSettingsRequestHandler>()
            .WithAgv(agv)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<Success>(new UpdateAgvsSettings(["unknown".ToGuid(), agv.CorrelationId], EfAgvSettingsProvider.DefaultSettings, "user")));
        (await fixture.TestHarness.Published.Any<UpdateAgvSettingsGroupReference>(m => m.Context.Message == new UpdateAgvSettingsGroupReference("unknown".ToGuid()))).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<UpdateAgvSettingsGroupReference>(m => m.Context.Message == new UpdateAgvSettingsGroupReference(agv.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ResetAgvSettingToDefault_ValidData_SettingReset()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<ResetAgvSettingToDefault, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<ResetAgvSettingToDefault, AgvSettingsRequestHandler>()
            .WithAgv(agv)
            .Build();

        var command = new ResetAgvSettingToDefault(agv.CorrelationId, "a known property name", "user");
        (await fixture.GetResponseAsync<Success>(command)).ShouldNotBeNull();
        fixture.VerifyAgvSettingsPropertyReset(agv.AgvSettingsGroup, command.Property);
    }

    [TestMethod]
    public async Task Consume_ResetAgvSettingToDefault_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<ResetAgvSettingToDefault, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<ResetAgvSettingToDefault, AgvSettingsRequestHandler>()
            .WithAgv(agv)
            .Build();

        var command = new ResetAgvSettingToDefault("unknown".ToGuid(), "a known property name", "user");
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<Success>(command));
    }

    [TestMethod]
    public async Task Consume_ResetAgvsSettingToDefault_ValidData_SettingReset()
    {
        List<TransportOrder.Domain.Models.Agv.Agv> agvs = [AgvGenerator.GenerateIdleAgv(), AgvGenerator.GenerateBusyAgv(index: 2)];
        AgvRequestsHandlerTestFixture<ResetAgvsSettingToDefault, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<ResetAgvsSettingToDefault, AgvSettingsRequestHandler>()
            .WithAgvs(agvs)
            .Build();

        var command = new ResetAgvsSettingToDefault(agvs.Select(a => a.CorrelationId).ToList(), "a known property name", "user");
        (await fixture.GetResponseAsync<Success>(command)).ShouldNotBeNull();
        foreach (var agv in agvs)
        {
            fixture.VerifyAgvSettingsPropertyReset(agv.AgvSettingsGroup, command.PropertyName);
        }
    }

    [TestMethod]
    public async Task Consume_ResetAgvsSettingToDefault_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<ResetAgvsSettingToDefault, AgvSettingsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<ResetAgvsSettingToDefault, AgvSettingsRequestHandler>()
            .WithAgv(agv)
            .Build();

        var command = new ResetAgvsSettingToDefault([agv.CorrelationId, "unknown".ToGuid()], "a known property name", "user");
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<Success>(command));
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
        Response<Success> response = await fixture.GetResponseAsync<Success>(new UpdateDefaultAgvSettings(settings, "user"));
        response.ShouldNotBeNull();
        fixture.VerifyAgvSettingsUpdated(group.GroupType, group.Identifier, settings);
    }
}