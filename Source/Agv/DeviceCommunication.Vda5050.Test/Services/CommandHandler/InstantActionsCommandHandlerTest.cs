using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class InstantActionsCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StartInstantAction_validData_PublishesMqttInstantActions()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), interfaceName: "uagv", protocolMajorVersion: "2");
        var fixture = new InstantActionsCommandHandlerTestFixture()
            .AddAgv(agv);

        var startInstantAction = new StartInstantAction{
            CorrelationId = Guid.NewGuid(), 
            AgvSerialNumber = agv.SerialNumber,  
            AgvInstantActionType = Vda5050Specifications.ActionTypes.StartCharging, 
            BlockingType = BlockingType.HARD};

        await fixture.PublishMessageAsync(startInstantAction);

        await fixture.ShouldHavePublishedAsync<MqttInstantActions>();

        var context = fixture.TestHarness.Published.Select<MqttInstantActions>().First().Context;
        context.RoutingKey().ShouldBe(agv.GetRoutingKey(Vda5050Specifications.Subtopics.InstantActions));
    }
}