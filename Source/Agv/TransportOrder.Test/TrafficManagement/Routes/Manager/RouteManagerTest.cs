using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Manager;
using Shouldly;
using CommonRouteStepStatus = Gebhardt.Acc.Common.Messages.Route.RouteSteps.RouteStepStatus;
using RouteStepState = Gebhardt.Acc.Common.Messages.Route.RouteSteps.RouteStepState;
using RouteStepStatus = Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.RouteStepStatus;

namespace Gebhardt.Acc.TransportOrder.Test.TrafficManagement.Routes.Manager;

[TestClass]
public class RouteManagerTest
{
    [TestMethod]
    public async Task InvokeRoute_AgvNotInAutomaticMode_PublishesActionFailedAndCreatesNoRoute()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(1, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0], IsInAutomaticMode = false };
        var request = new RouteRequest(agv, [nodes[1]], Guid.NewGuid());

        fixture.RouteManager.InvokeRoute(request);

        var actionFailed = await fixture.GetFirstPublishedMessageAsync<ActionFailedByAgv>();
        actionFailed.CorrelationId.ShouldBe(request.ActionCorrelationId);
        actionFailed.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        fixture.RouteManager.Routes.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task InvokeRoute_RouteForAgvAlreadyExists_PublishesActionFailedAndKeepsExistingRoute()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(1, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        fixture.RouteManager.InvokeRoute(new RouteRequest(agv, [nodes[1]], Guid.NewGuid()));
        var existingRoute = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        var secondRequest = new RouteRequest(agv, [nodes[1]], Guid.NewGuid());

        fixture.RouteManager.InvokeRoute(secondRequest);

        var actionFailed = await fixture.GetFirstPublishedMessageAsync<ActionFailedByAgv>();
        actionFailed.CorrelationId.ShouldBe(secondRequest.ActionCorrelationId);
        fixture.RouteManager.Routes.ShouldHaveSingleItem().ShouldBeSameAs(existingRoute);
    }

    [TestMethod]
    public async Task InvokeRoute_AgvWithoutCurrentRoutingNode_PublishesActionFailed()
    {
        var fixture = new RouteManagerTestFixture();
        var targetNode = RouteManagerTestFixture.CreateIsolatedNode();
        var agv = new RoutingAgvStub { CurrentRoutingNode = null };
        var request = new RouteRequest(agv, [targetNode], Guid.NewGuid());

        fixture.RouteManager.InvokeRoute(request);

        var actionFailed = await fixture.GetFirstPublishedMessageAsync<ActionFailedByAgv>();
        actionFailed.CorrelationId.ShouldBe(request.ActionCorrelationId);
        fixture.RouteManager.Routes.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task InvokeRoute_NoRouteFound_PublishesActionFailed()
    {
        var fixture = new RouteManagerTestFixture();
        var startNode = RouteManagerTestFixture.CreateIsolatedNode();
        var unreachableNode = RouteManagerTestFixture.CreateIsolatedNode(10);
        var agv = new RoutingAgvStub { CurrentRoutingNode = startNode };
        var request = new RouteRequest(agv, [unreachableNode], Guid.NewGuid());

        fixture.RouteManager.InvokeRoute(request);

        var actionFailed = await fixture.GetFirstPublishedMessageAsync<ActionFailedByAgv>();
        actionFailed.CorrelationId.ShouldBe(request.ActionCorrelationId);
        fixture.RouteManager.Routes.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task InvokeRoute_ValidRequest_AddsRouteAndPublishesRoutePlannedAndActionStarted()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(2, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        var request = new RouteRequest(agv, [nodes[2]], Guid.NewGuid());

        fixture.RouteManager.InvokeRoute(request);

        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        route.ActionCorrelationId.ShouldBe(request.ActionCorrelationId);

        var routePlanned = await fixture.GetFirstPublishedMessageAsync<RoutePlanned>();
        routePlanned.RouteId.ShouldBe(route.Id);
        routePlanned.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        routePlanned.RouteSteps.Select(s => s.RouteStepId).ShouldBe(route.Steps.Select(s => s.RouteStepId));

        var actionStarted = await fixture.GetFirstPublishedMessageAsync<ActionStartedByAgv>();
        actionStarted.CorrelationId.ShouldBe(request.ActionCorrelationId);
        actionStarted.AgvCorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task InvokeRoute_ValidRequest_ReservesAndReleasesStep()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(2, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };

        fixture.RouteManager.InvokeRoute(new RouteRequest(agv, [nodes[2]], Guid.NewGuid()));

        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        route.Steps.ShouldAllBe(s => s.Status == RouteStepStatus.Released);
        route.Steps.ShouldAllBe(s => s.Reservations.Count == 2);

        var routeStepsReleased = await fixture.GetFirstPublishedMessageAsync<RouteStepsReleased>();
        routeStepsReleased.RouteId.ShouldBe(route.Id);
        routeStepsReleased.RouteStepIds.Length.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public async Task InvokeRoute_RouteLongerThanMaxReservationLength_ReleasesOnlyStepsWithinLimit()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(5, 3);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };

        fixture.RouteManager.InvokeRoute(new RouteRequest(agv, [nodes[5]], Guid.NewGuid()));

        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        List<RouteStep> releasedSteps = route.Steps.Where(s => s.Status == RouteStepStatus.Released).ToList();
        releasedSteps.Count.ShouldBe(3);
        route.Steps.Skip(3).ShouldAllBe(s => s.Status == RouteStepStatus.Planned);

        var routeStepsReleased = await fixture.GetFirstPublishedMessageAsync<RouteStepsReleased>();
        routeStepsReleased.RouteStepIds.ShouldBe(releasedSteps.Select(s => s.RouteStepId));
    }

    [TestMethod]
    public async Task InvokeRoute_NextStepBlockedByOtherAgv_ReleasesOnlyStepsBeforeBlockedStep()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, RoutingEdge[] edges) = RouteManagerTestFixture.CreateChain(3, 1);
        fixture.ReservationManager.TryReserve(edges[1], Guid.NewGuid(), out _).ShouldBeTrue();
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };

        fixture.RouteManager.InvokeRoute(new RouteRequest(agv, [nodes[3]], Guid.NewGuid()));

        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        route.Steps.First().Status.ShouldBe(RouteStepStatus.Released);
        route.Steps.Skip(1).ShouldAllBe(s => s.Status == RouteStepStatus.Planned);

        var routeStepsReleased = await fixture.GetFirstPublishedMessageAsync<RouteStepsReleased>();
        routeStepsReleased.RouteStepIds.ShouldBe([route.Steps.First().RouteStepId]);
    }

    [TestMethod]
    public async Task InvokeRoute_StartNodeIsTargetNode_CompletesRouteImmediately()
    {
        var fixture = new RouteManagerTestFixture();
        var startNode = RouteManagerTestFixture.CreateIsolatedNode();
        var agv = new RoutingAgvStub { CurrentRoutingNode = startNode };
        var request = new RouteRequest(agv, [startNode], Guid.NewGuid());

        fixture.RouteManager.InvokeRoute(request);

        var actionCompleted = await fixture.GetFirstPublishedMessageAsync<ActionCompletedByAgv>();
        actionCompleted.CorrelationId.ShouldBe(request.ActionCorrelationId);
        actionCompleted.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        fixture.RouteManager.Routes.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task AbortRoute_UnknownAgv_PublishesActionAbortionFailed()
    {
        var fixture = new RouteManagerTestFixture();
        var agvCorrelationId = Guid.NewGuid();
        var actionCorrelationId = Guid.NewGuid();

        fixture.RouteManager.AbortRoute(agvCorrelationId, actionCorrelationId);

        var abortionFailed = await fixture.GetFirstPublishedMessageAsync<ActionAbortionFailedByAgv>();
        abortionFailed.CorrelationId.ShouldBe(actionCorrelationId);
    }

    [TestMethod]
    public void AbortRoute_ActiveRoute_FailsOpenStepsAndReleasesTheirReservations()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, RoutingEdge[] edges) = RouteManagerTestFixture.CreateChain(2, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        var request = new RouteRequest(agv, [nodes[2]], Guid.NewGuid());
        fixture.RouteManager.InvokeRoute(request);
        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();

        fixture.RouteManager.AbortRoute(agv.CorrelationId, request.ActionCorrelationId);

        fixture.RouteManager.Routes.ShouldBeEmpty();
        route.Steps.ShouldAllBe(s => s.Status == RouteStepStatus.Failed);
        route.Steps.ShouldAllBe(s => s.Reservations.Count == 0);
        fixture.ReservationManager.TryReserve(edges[0], Guid.NewGuid(), out _).ShouldBeTrue();
        fixture.ReservationManager.TryReserve(edges[1], Guid.NewGuid(), out _).ShouldBeTrue();
    }

    [TestMethod]
    public async Task AbortRoute_ActiveRoute_RemovesRouteAndPublishesRouteAborted()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(2, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        var request = new RouteRequest(agv, [nodes[2]], Guid.NewGuid());
        fixture.RouteManager.InvokeRoute(request);
        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();

        fixture.RouteManager.AbortRoute(agv.CorrelationId, request.ActionCorrelationId);

        fixture.RouteManager.Routes.ShouldBeEmpty();

        var routeAborted = await fixture.GetFirstPublishedMessageAsync<RouteAborted>();
        routeAborted.RouteId.ShouldBe(route.Id);
        routeAborted.AgvCorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public void ProcessRouteSteps_UnknownAgv_LeavesRoutesUnchanged()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(2, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        fixture.RouteManager.InvokeRoute(new RouteRequest(agv, [nodes[2]], Guid.NewGuid()));
        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        var firstStep = route.Steps.First();

        fixture.RouteManager.ProcessRouteSteps(new RouteStepsProcessed(route.Id, Guid.NewGuid(),
            [new RouteStepState(firstStep.RouteStepId, CommonRouteStepStatus.Completed)]));

        fixture.RouteManager.Routes.ShouldHaveSingleItem().ShouldBeSameAs(route);
        route.Steps.ShouldAllBe(s => s.Status == RouteStepStatus.Released);
    }

    [TestMethod]
    public void ProcessRouteSteps_RouteIdMismatch_LeavesRouteUnchanged()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(2, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        fixture.RouteManager.InvokeRoute(new RouteRequest(agv, [nodes[2]], Guid.NewGuid()));
        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        var firstStep = route.Steps.First();

        fixture.RouteManager.ProcessRouteSteps(new RouteStepsProcessed(Guid.NewGuid(), agv.CorrelationId,
            [new RouteStepState(firstStep.RouteStepId, CommonRouteStepStatus.Completed)]));

        fixture.RouteManager.Routes.ShouldHaveSingleItem().ShouldBeSameAs(route);
        route.Steps.ShouldAllBe(s => s.Status == RouteStepStatus.Released);
    }

    [TestMethod]
    public void ProcessRouteSteps_StepAcknowledged_SetsStatusAndKeepsReservations()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(2, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        fixture.RouteManager.InvokeRoute(new RouteRequest(agv, [nodes[2]], Guid.NewGuid()));
        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        var firstStep = route.Steps.First();

        fixture.RouteManager.ProcessRouteSteps(new RouteStepsProcessed(route.Id, agv.CorrelationId,
            [new RouteStepState(firstStep.RouteStepId, CommonRouteStepStatus.Acknowledged)]));

        firstStep.Status.ShouldBe(RouteStepStatus.Acknowledged);
        firstStep.Reservations.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task ProcessRouteSteps_IntermediateStepCompleted_ReleasesFollowingStep()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(5, 3);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        fixture.RouteManager.InvokeRoute(new RouteRequest(agv, [nodes[5]], Guid.NewGuid()));
        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        List<RouteStep> steps = [.. route.Steps];
        await fixture.GetFirstPublishedMessageAsync<RouteStepsReleased>();

        fixture.RouteManager.ProcessRouteSteps(new RouteStepsProcessed(route.Id, agv.CorrelationId,
            [new RouteStepState(steps[0].RouteStepId, CommonRouteStepStatus.Completed)]));

        steps[0].Status.ShouldBe(RouteStepStatus.Completed);
        steps[0].Reservations.ShouldBeEmpty();
        steps[3].Status.ShouldBe(RouteStepStatus.Released);
        steps[4].Status.ShouldBe(RouteStepStatus.Planned);

        var routeStepsReleased = await fixture.GetFirstPublishedMessageAsync<RouteStepsReleased>(2);
        routeStepsReleased.RouteStepIds.ShouldBe([steps[3].RouteStepId]);
    }

    [TestMethod]
    public async Task ProcessRouteSteps_LastStepCompleted_CompletesRoute()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(1, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        var request = new RouteRequest(agv, [nodes[1]], Guid.NewGuid());
        fixture.RouteManager.InvokeRoute(request);
        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        var lastStep = route.Steps.Last();

        fixture.RouteManager.ProcessRouteSteps(new RouteStepsProcessed(route.Id, agv.CorrelationId,
            [new RouteStepState(lastStep.RouteStepId, CommonRouteStepStatus.Completed)]));

        lastStep.Status.ShouldBe(RouteStepStatus.Completed);
        fixture.RouteManager.Routes.ShouldBeEmpty();

        var actionCompleted = await fixture.GetFirstPublishedMessageAsync<ActionCompletedByAgv>();
        actionCompleted.CorrelationId.ShouldBe(request.ActionCorrelationId);
        actionCompleted.AgvCorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task ProcessRouteSteps_StepFailed_PublishesActionFailedAndAbortsRoute()
    {
        var fixture = new RouteManagerTestFixture();
        (RoutingNode[] nodes, _) = RouteManagerTestFixture.CreateChain(3, 1);
        var agv = new RoutingAgvStub { CurrentRoutingNode = nodes[0] };
        var request = new RouteRequest(agv, [nodes[3]], Guid.NewGuid());
        fixture.RouteManager.InvokeRoute(request);
        var route = fixture.RouteManager.Routes.ShouldHaveSingleItem();
        var firstStep = route.Steps.First();

        fixture.RouteManager.ProcessRouteSteps(new RouteStepsProcessed(route.Id, agv.CorrelationId,
            [new RouteStepState(firstStep.RouteStepId, CommonRouteStepStatus.Failed, "TestFailure")]));

        route.Steps.ShouldAllBe(s => s.Status == RouteStepStatus.Failed);
        fixture.RouteManager.Routes.ShouldBeEmpty();

        var actionFailed = await fixture.GetFirstPublishedMessageAsync<ActionFailedByAgv>();
        actionFailed.CorrelationId.ShouldBe(request.ActionCorrelationId);

        var routeAborted = await fixture.GetFirstPublishedMessageAsync<RouteAborted>();
        routeAborted.RouteId.ShouldBe(route.Id);

        var actionAborted = await fixture.GetFirstPublishedMessageAsync<ActionAbortedByAgv>();
        actionAborted.CorrelationId.ShouldBe(request.ActionCorrelationId);
    }
}
