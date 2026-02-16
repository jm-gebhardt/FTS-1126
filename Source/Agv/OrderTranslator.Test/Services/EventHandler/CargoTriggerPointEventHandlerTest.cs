using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.OrderTranslator.Test.Helper;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.EventHandler;

[TestClass]
public class CargoTriggerPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_TargetNeedsCargoReporting_PublishesCargoReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = OrderGenerator.GetActionGroupFromOrder(order, OrderGenerator.Ps01, OrderGenerator.Apl1);

        var fixture = new CargoTriggerPointEventHandlerTestFixture()
            .WithStation(OrderGenerator.Apl1);

        await fixture.PublishMessageAsync(new AgvPassedCargoTriggerPoint(Guid.NewGuid(), actionGroup.CorrelationId, actionGroup.Actions.First(a => a.ActionType == ActionType.Drop), true));

        await fixture.ShouldHavePublishedAsync<CargoReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_TargetDoesNotNeedCargoReporting_DoesNotPublishCargoReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = OrderGenerator.GetActionGroupFromOrder(order, OrderGenerator.Ps01, OrderGenerator.Apl1);

        var fixture = new CargoTriggerPointEventHandlerTestFixture()
            .WithStation(OrderGenerator.Ps01);

        await fixture.PublishMessageAsync(new AgvPassedCargoTriggerPoint(Guid.NewGuid(), actionGroup.CorrelationId, actionGroup.Actions.First(a => a.ActionType == ActionType.Pick), false));

        await fixture.ShouldNotHavePublishedAsync<CargoReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_MoveToCoordinate_DoesNotPublishCargoReportingRequested()
    {
        var actionGroup = AgvActionGenerator.GenerateMoveToCoordinateActionGroup();

        var fixture = new CargoTriggerPointEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new AgvPassedCargoTriggerPoint(Guid.NewGuid(), actionGroup.CorrelationId, actionGroup.Actions.First(a => a.ActionType == ActionType.Move), true));

        await fixture.ShouldNotHavePublishedAsync<CargoReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_NoAction_DoesNotPublishCargoReportingRequested()
    {
        var fixture = new CargoTriggerPointEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new AgvPassedCargoTriggerPoint(Guid.NewGuid(), Guid.NewGuid(), null, true));

        await fixture.ShouldNotHavePublishedAsync<CargoReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_CargoMismatch_DoesNotPublishCargoReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = OrderGenerator.GetActionGroupFromOrder(order, OrderGenerator.Ps01, OrderGenerator.Apl1);

        var fixture = new CargoTriggerPointEventHandlerTestFixture()
            .WithStation(OrderGenerator.Apl1);

        await fixture.PublishMessageAsync(new AgvPassedCargoTriggerPoint(Guid.NewGuid(), actionGroup.CorrelationId, actionGroup.Actions.First(a => a.ActionType == ActionType.Drop), false));

        await fixture.ShouldNotHavePublishedAsync<CargoReportingRequested>();
    }
}