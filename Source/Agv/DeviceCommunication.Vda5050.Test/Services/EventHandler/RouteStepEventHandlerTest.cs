using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

[TestClass]
public class RouteStepEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ProgressReported_PublishesRouteStepsProcessedWithChangedSteps()
    {
        var orderId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var agvCorrelationId = Guid.NewGuid();
        var changedStep = new RouteStepState(Guid.NewGuid(), RouteStepStatus.Completed);
        var fixture = new RouteStepEventHandlerTestFixture()
            .AddProgress(orderId, new RouteStepProgress(routeId, agvCorrelationId, [changedStep], false));

        await fixture.PublishMessageAsync(new OrderStateReceived(orderId, OrderGenerator.GenerateOrderState(orderId.ToString(), 0, 0, false)));

        var published = await fixture.GetFirstPublishedMessageAsync<RouteStepsProcessed>();
        published.RouteId.ShouldBe(routeId);
        published.AgvCorrelationId.ShouldBe(agvCorrelationId);
        published.RouteStepStates.ShouldContain(changedStep);
    }

    [TestMethod]
    public async Task Consume_NoProgress_PublishesNothing()
    {
        var orderId = Guid.NewGuid();
        var fixture = new RouteStepEventHandlerTestFixture().AddProgress(orderId, null);

        await fixture.PublishMessageAsync(new OrderStateReceived(orderId, OrderGenerator.GenerateOrderState(orderId.ToString(), 0, 0, false)));

        await fixture.ShouldNotHavePublishedAsync<RouteStepsProcessed>();
    }

    [TestMethod]
    public async Task Consume_OrderCompleted_ClearsAgvCurrentOrder()
    {
        var orderId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: orderId, currentFleetManagerActionId: routeId);
        var changedStep = new RouteStepState(Guid.NewGuid(), RouteStepStatus.Completed);
        var fixture = new RouteStepEventHandlerTestFixture()
            .AddAgv(agv)
            .AddProgress(orderId, new RouteStepProgress(routeId, agv.CorrelationId, [changedStep], true));

        await fixture.PublishMessageAsync(new OrderStateReceived(orderId, OrderGenerator.GenerateOrderState(orderId.ToString(), 0, 0, false)));

        agv.CurrentOrder.ShouldBeNull();
        agv.CurrentFleetManagerActionId.ShouldBeNull();
        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(agv, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_OrderNotCompleted_DoesNotClearAgvCurrentOrder()
    {
        var orderId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: orderId, currentFleetManagerActionId: routeId);
        var changedStep = new RouteStepState(Guid.NewGuid(), RouteStepStatus.Started);
        var fixture = new RouteStepEventHandlerTestFixture()
            .AddAgv(agv)
            .AddProgress(orderId, new RouteStepProgress(routeId, agv.CorrelationId, [changedStep], false));

        await fixture.PublishMessageAsync(new OrderStateReceived(orderId, OrderGenerator.GenerateOrderState(orderId.ToString(), 0, 0, false)));

        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
