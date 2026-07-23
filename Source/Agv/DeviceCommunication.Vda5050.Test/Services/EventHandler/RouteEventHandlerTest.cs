using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

[TestClass]
public class RouteEventHandlerTest
{
    [TestMethod]
    public async Task Consume_RoutePlanned_NoRouteAction_PublishesNothing()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new RouteEventHandlerTestFixture().AddAgv(agv).AddResult(new NoRouteAction());

        await fixture.PublishMessageAsync(new RoutePlanned(Guid.NewGuid(), agv.CorrelationId, []));

        await fixture.ShouldNotHavePublishedAsync<StartOrder>();
        await fixture.ShouldNotHavePublishedAsync<OrderUpdateRequested>();
        await fixture.ShouldNotHavePublishedAsync<OrderCancellationRequested>();
    }

    [TestMethod]
    public async Task Consume_RouteStepsReleased_StartRouteOrder_PublishesStartOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var routeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var fixture = new RouteEventHandlerTestFixture().AddAgv(agv).AddResult(new StartRouteOrder(routeId, agv.CorrelationId, orderId, order));

        await fixture.PublishMessageAsync(new RouteStepsReleased(routeId, agv.CorrelationId, []));

        var published = await fixture.GetFirstPublishedMessageAsync<StartOrder>();
        published.CorrelationId.ShouldBe(orderId);
        fixture.VerifyAgvUpdated();
    }

    [TestMethod]
    public async Task Consume_RouteUpdated_UpdateRouteOrder_PublishesOrderUpdateRequested()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var routeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var fixture = new RouteEventHandlerTestFixture().AddAgv(agv).AddResult(new UpdateRouteOrder(routeId, agv.CorrelationId, orderId, order));

        await fixture.PublishMessageAsync(new RouteUpdated(routeId, agv.CorrelationId, []));

        await fixture.ShouldHaveConsumedAsync<RouteUpdated>();
        var published = await fixture.GetFirstPublishedMessageAsync<OrderUpdateRequested>();
        published.CorrelationId.ShouldBe(orderId);
        published.CurrentFleetManagerActionId.ShouldBe(routeId);
    }

    [TestMethod]
    public async Task Consume_RouteAborted_CancelRouteOrder_PublishesOrderCancellationRequested()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var routeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var fixture = new RouteEventHandlerTestFixture().AddAgv(agv).AddResult(new CancelRouteOrder(routeId, agv.CorrelationId, orderId));

        await fixture.PublishMessageAsync(new RouteAborted(routeId, agv.CorrelationId));

        var published = await fixture.GetFirstPublishedMessageAsync<OrderCancellationRequested>();
        published.CorrelationId.ShouldBe(orderId);
    }

    [TestMethod]
    public async Task Consume_RouteStepsReleased_RouteCoordinationFailed_PublishesNothing()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var routeId = Guid.NewGuid();
        var fixture = new RouteEventHandlerTestFixture().AddAgv(agv).AddResult(new RouteCoordinationFailed(RouteExecutionErrors.RouteStepNotFound));

        await fixture.PublishMessageAsync(new RouteStepsReleased(routeId, agv.CorrelationId, [Guid.NewGuid()]));

        await fixture.ShouldNotHavePublishedAsync<StartOrder>();
        await fixture.ShouldNotHavePublishedAsync<OrderUpdateRequested>();
    }
}
