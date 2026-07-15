using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

[TestClass]
public class ActionEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionAborted_ChargeAction_PublishesStopChargingRequested()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new ActionEventHandlerTestFixture()
            .AddAgv(agv);

        var action = AgvActionGenerator.GenerateChargeAction();

        await fixture.PublishMessageAsync(new ActionAborted(agv.CorrelationId, action));

        await fixture.ShouldHavePublishedAsync<StopChargingRequested>();
    }

    [DataRow(ActionType.Drop)]
    [DataRow(ActionType.Pick)]
    [DataRow(ActionType.Move)]
    [TestMethod]
    public async Task Consume_ActionAborted_NonChargeAction_PublishesOrderCancellationRequested(ActionType type)
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new ActionEventHandlerTestFixture()
            .AddAgv(agv);

        var action = AgvActionGenerator.GenerateAction(actionType: type);

        await fixture.PublishMessageAsync(new ActionAborted(agv.CorrelationId, action));

        await fixture.ShouldHavePublishedAsync<OrderCancellationRequested>();
    }

    [TestMethod]
    public async Task Consume_ActionCompletedByAgv_ValidData_PublishesClearAgvCurrentOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid());
        var fixture = new ActionEventHandlerTestFixture()
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new ActionCompletedByAgv(agv.CurrentOrder!.Value, agv.CorrelationId));

        var published = await fixture.GetFirstPublishedMessageAsync<ClearAgvCurrentOrder>();
        published.CorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_ActionAbortedByAgv_ValidData_PublishesClearAgvCurrentOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid());
        var fixture = new ActionEventHandlerTestFixture()
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new ActionAbortedByAgv(agv.CurrentOrder!.Value, agv.CorrelationId));

        var published = await fixture.GetFirstPublishedMessageAsync<ClearAgvCurrentOrder>();
        published.CorrelationId.ShouldBe(agv.CorrelationId);
    }
}