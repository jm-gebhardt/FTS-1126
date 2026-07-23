using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services.Routes;

[TestClass]
public class RouteExecutionCoordinatorTest
{
    private readonly Mock<IRouteExecutionRepository> _routeExecutionRepository = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IRouteStepTranslator> _routeStepTranslator = new();
    private readonly Agv _agv;
    private readonly RouteExecutionCoordinator _coordinator;

    public RouteExecutionCoordinatorTest()
    {
        _agv = AgvGenerator.Generate(Guid.NewGuid());
        _agvRepository.Setup(x => x.GetRequiredAgvAsync(_agv.CorrelationId)).ReturnsAsync(_agv);
        _routeStepTranslator.Setup(x => x.Translate(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<RouteStepExecution>>()))
            .ReturnsAsync(RouteGenerator.GenerateFromNodeIds(["A", "B"]));
        _coordinator = new RouteExecutionCoordinator(_routeExecutionRepository.Object, _agvRepository.Object, _routeStepTranslator.Object);
    }

    private static RouteStep[] CreateRouteSteps(int count = 2)
    {
        return Enumerable.Range(0, count).Select(_ => new RouteStep(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null)).ToArray();
    }

    [TestMethod]
    public async Task PlanRouteAsync_NoExistingRoute_AddsANewRouteExecution()
    {
        var routeId = Guid.NewGuid();
        _routeExecutionRepository.Setup(x => x.GetAsync(routeId)).ReturnsAsync((RouteExecution?)null);

        await _coordinator.PlanRouteAsync(routeId, _agv.CorrelationId, CreateRouteSteps());

        _routeExecutionRepository.Verify(x => x.AddAsync(It.Is<RouteExecution>(r => r.RouteId == routeId && r.AgvCorrelationId == _agv.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task PlanRouteAsync_NoExistingRoute_AllStepsAreUnreleased()
    {
        var routeId = Guid.NewGuid();
        _routeExecutionRepository.Setup(x => x.GetAsync(routeId)).ReturnsAsync((RouteExecution?)null);
        RouteExecution? added = null;
        _routeExecutionRepository.Setup(x => x.AddAsync(It.IsAny<RouteExecution>())).Callback<RouteExecution>(r => added = r);

        await _coordinator.PlanRouteAsync(routeId, _agv.CorrelationId, CreateRouteSteps());

        added!.Steps.ShouldAllBe(s => !s.Released);
    }

    [TestMethod]
    public async Task PlanRouteAsync_NoExistingRoute_ReturnsNoRouteAction()
    {
        var routeId = Guid.NewGuid();
        _routeExecutionRepository.Setup(x => x.GetAsync(routeId)).ReturnsAsync((RouteExecution?)null);

        var result = await _coordinator.PlanRouteAsync(routeId, _agv.CorrelationId, CreateRouteSteps());

        result.ShouldBeOfType<NoRouteAction>();
    }

    [TestMethod]
    public async Task PlanRouteAsync_ExistingRoute_UpdatesInsteadOfAdding()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        _routeExecutionRepository.Setup(x => x.GetAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        await _coordinator.PlanRouteAsync(routeExecution.RouteId, _agv.CorrelationId, CreateRouteSteps());

        _routeExecutionRepository.Verify(x => x.UpdateAsync(routeExecution), Times.Once);
        _routeExecutionRepository.Verify(x => x.AddAsync(It.IsAny<RouteExecution>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateRouteAsync_BeforeAnyRelease_ReturnsNoRouteAction()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.UpdateRouteAsync(routeExecution.RouteId, CreateRouteSteps());

        result.ShouldBeOfType<NoRouteAction>();
    }

    [TestMethod]
    public async Task UpdateRouteAsync_OrderActiveAndNotCompleted_ReturnsUpdateRouteOrderForCurrentOrderId()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        var currentOrderId = routeExecution.MintNextOrderId();
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.UpdateRouteAsync(routeExecution.RouteId, CreateRouteSteps());

        var update = result.ShouldBeOfType<UpdateRouteOrder>();
        update.OrderId.ShouldBe(currentOrderId);
        update.RouteId.ShouldBe(routeExecution.RouteId);
    }

    [TestMethod]
    public async Task UpdateRouteAsync_OrderActiveAndCompleted_MintsAFreshOrderIdAndReturnsStartRouteOrder()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        var previousOrderId = routeExecution.MintNextOrderId();
        routeExecution.CurrentOrderCompleted = true;
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.UpdateRouteAsync(routeExecution.RouteId, CreateRouteSteps());

        var start = result.ShouldBeOfType<StartRouteOrder>();
        start.OrderId.ShouldNotBe(previousOrderId);
        routeExecution.OrderIdHistory.ShouldContain(previousOrderId);
    }

    [TestMethod]
    public async Task ReleaseRouteStepsAsync_UnknownStepId_ReturnsRouteCoordinationFailed()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        routeExecution.ApplyPlanned([new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null)]);
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.ReleaseRouteStepsAsync(routeExecution.RouteId, [Guid.NewGuid()]);

        result.ShouldBeOfType<RouteCoordinationFailed>();
    }

    [TestMethod]
    public async Task ReleaseRouteStepsAsync_UnknownStepId_NeverCallsTranslator()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        routeExecution.ApplyPlanned([new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null)]);
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        await _coordinator.ReleaseRouteStepsAsync(routeExecution.RouteId, [Guid.NewGuid()]);

        _routeStepTranslator.Verify(x => x.Translate(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<RouteStepExecution>>()), Times.Never);
    }

    [TestMethod]
    public async Task ReleaseRouteStepsAsync_FirstRelease_MintsOrderIdAndReturnsStartRouteOrder()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        var step = new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
        routeExecution.ApplyPlanned([step]);
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.ReleaseRouteStepsAsync(routeExecution.RouteId, [step.RouteStepId]);

        var start = result.ShouldBeOfType<StartRouteOrder>();
        start.OrderId.ShouldBe(routeExecution.CurrentOrderId!.Value);
        step.Released.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ReleaseRouteStepsAsync_SubsequentRelease_ReturnsUpdateRouteOrderWithSameOrderId()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        var stepA = new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
        var stepB = new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
        routeExecution.ApplyPlanned([stepA, stepB]);
        routeExecution.ReleaseSteps([stepA.RouteStepId]);
        var mintedOrderId = routeExecution.MintNextOrderId();
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.ReleaseRouteStepsAsync(routeExecution.RouteId, [stepB.RouteStepId]);

        var update = result.ShouldBeOfType<UpdateRouteOrder>();
        update.OrderId.ShouldBe(mintedOrderId);
    }

    [TestMethod]
    public async Task AbortRouteAsync_NoActiveOrder_ReturnsNoRouteAction()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.AbortRouteAsync(routeExecution.RouteId);

        result.ShouldBeOfType<NoRouteAction>();
    }

    [TestMethod]
    public async Task AbortRouteAsync_ActiveOrder_ReturnsCancelRouteOrder()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        var orderId = routeExecution.MintNextOrderId();
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.AbortRouteAsync(routeExecution.RouteId);

        var cancel = result.ShouldBeOfType<CancelRouteOrder>();
        cancel.OrderId.ShouldBe(orderId);
    }

    [TestMethod]
    public async Task AbortRouteAsync_MarksRouteExecutionAsAborted()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(routeExecution.RouteId)).ReturnsAsync(routeExecution);

        await _coordinator.AbortRouteAsync(routeExecution.RouteId);

        routeExecution.LifecycleStatus.ShouldBe(RouteExecutionStatus.Aborted);
    }

    [TestMethod]
    public async Task DetermineRouteStepProgress_OrderIdNotOwnedByAnyRoute_ReturnsNull()
    {
        var orderId = Guid.NewGuid();
        _routeExecutionRepository.Setup(x => x.GetByCurrentOrderIdAsync(orderId)).ReturnsAsync((RouteExecution?)null);

        var result = await _coordinator.DetermineRouteStepProgress(orderId, OrderGenerator.GenerateOrderState("order", 0, 0, false));

        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task DetermineRouteStepProgress_NodeReached_MarksTravelStepCompletedAndReturnsProgress()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        var step = new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null) { NodeSequenceId = 2, Status = RouteStepStatus.Acknowledged };
        routeExecution.ApplyPlanned([step]);
        var orderId = routeExecution.MintNextOrderId();
        _routeExecutionRepository.Setup(x => x.GetByCurrentOrderIdAsync(orderId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.DetermineRouteStepProgress(orderId, OrderGenerator.GenerateOrderState("order", 0, 2, false));

        result.ShouldNotBeNull();
        result.RouteId.ShouldBe(routeExecution.RouteId);
        step.Status.ShouldBe(RouteStepStatus.Completed);
    }

    [TestMethod]
    public async Task DetermineRouteStepProgress_NothingChanged_ReturnsNull()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        var step = new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null) { NodeSequenceId = 2, Status = RouteStepStatus.Completed };
        routeExecution.ApplyPlanned([step]);
        var orderId = routeExecution.MintNextOrderId();
        _routeExecutionRepository.Setup(x => x.GetByCurrentOrderIdAsync(orderId)).ReturnsAsync(routeExecution);

        var result = await _coordinator.DetermineRouteStepProgress(orderId, OrderGenerator.GenerateOrderState("order", 0, 2, false));

        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task DetermineRouteStepProgress_AllStepsCompleted_MarksCurrentOrderCompleted()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), _agv.CorrelationId);
        var step = new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null) { NodeSequenceId = 2, Status = RouteStepStatus.Acknowledged };
        routeExecution.ApplyPlanned([step]);
        var orderId = routeExecution.MintNextOrderId();
        _routeExecutionRepository.Setup(x => x.GetByCurrentOrderIdAsync(orderId)).ReturnsAsync(routeExecution);

        await _coordinator.DetermineRouteStepProgress(orderId, OrderGenerator.GenerateOrderState("order", 0, 2, false));

        routeExecution.CurrentOrderCompleted.ShouldBeTrue();
    }
}
