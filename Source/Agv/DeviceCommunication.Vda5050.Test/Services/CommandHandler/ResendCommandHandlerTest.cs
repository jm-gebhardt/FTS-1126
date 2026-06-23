using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class ResendCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_RequestAgvData_AgvExists_PublishesMqttInstantActionsWithFactsheetRequest()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), interfaceName: "uagv", protocolMajorVersion: "2");
        var fixture = new ResendCommandHandlerTestFixture().AddAgv(agv);

        await fixture.PublishMessageAsync(new RequestAgvData(agv.CorrelationId, agv.SerialNumber));

        await fixture.ShouldHavePublishedAsync<MqttInstantActions>();
        PublishContext<MqttInstantActions> context = fixture.TestHarness.Published.Select<MqttInstantActions>().First().Context;
        context.Message.Actions.ShouldHaveSingleItem();
        context.Message.Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.RequestFactsheet);
    }

    [TestMethod]
    public async Task Consume_RequestAgvData_AgvExists_PublishesToInstantActionsRoutingKey()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), interfaceName: "uagv", protocolMajorVersion: "2");
        var fixture = new ResendCommandHandlerTestFixture().AddAgv(agv);

        await fixture.PublishMessageAsync(new RequestAgvData(agv.CorrelationId, agv.SerialNumber));

        PublishContext<MqttInstantActions> context = fixture.TestHarness.Published.Select<MqttInstantActions>().First().Context;
        context.RoutingKey().ShouldBe(agv.GetRoutingKey(Vda5050Specifications.Subtopics.InstantActions));
    }

    [TestMethod]
    public async Task Consume_RequestAgvData_AgvExists_UpdatesFactSheetRequestCache()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), interfaceName: "uagv", protocolMajorVersion: "2");
        var fixture = new ResendCommandHandlerTestFixture().AddAgv(agv);

        await fixture.PublishMessageAsync(new RequestAgvData(agv.CorrelationId, agv.SerialNumber));

        fixture.FactSheetRequestCache.ConcurrentDict.ContainsKey(agv.CorrelationId).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_RequestAgvData_AgvNotFound_DoesNotPublishMqttInstantActions()
    {
        var fixture = new ResendCommandHandlerTestFixture().AddAgv(null);

        await fixture.PublishMessageAsync(new RequestAgvData(Guid.NewGuid(), "serial number"));

        await fixture.ShouldNotHavePublishedAsync<MqttInstantActions>();
    }
}
