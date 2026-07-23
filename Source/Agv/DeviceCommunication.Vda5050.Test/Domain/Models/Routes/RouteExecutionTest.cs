using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Models.Routes;

[TestClass]
public class RouteExecutionTest
{
    private static RouteStepExecution CreateStep()
    {
        return new RouteStepExecution(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
    }

    private static RouteExecution CreatePlannedRouteExecution(out RouteStepExecution[] steps, int stepCount = 3)
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());
        steps = Enumerable.Range(0, stepCount).Select(_ => CreateStep()).ToArray();
        routeExecution.ApplyPlanned(steps);
        return routeExecution;
    }

    [TestMethod]
    public void ApplyPlanned_AllStepsStartUnreleased()
    {
        var routeExecution = CreatePlannedRouteExecution(out _);

        routeExecution.Steps.ShouldAllBe(s => !s.Released);
    }

    [TestMethod]
    public void ApplyPlanned_PreservesStepOrder()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());
        RouteStepExecution[] steps = [CreateStep(), CreateStep(), CreateStep()];

        routeExecution.ApplyPlanned(steps);

        routeExecution.Steps.Select(s => s.RouteStepId).ShouldBe(steps.Select(s => s.RouteStepId));
    }

    [TestMethod]
    public void ReleaseSteps_ContiguousPrefixFromStart_ReturnsSuccess()
    {
        var routeExecution = CreatePlannedRouteExecution(out RouteStepExecution[] steps);

        var result = routeExecution.ReleaseSteps([steps[0].RouteStepId, steps[1].RouteStepId]);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public void ReleaseSteps_ContiguousPrefixFromStart_ReleasesOnlyNamedSteps()
    {
        var routeExecution = CreatePlannedRouteExecution(out RouteStepExecution[] steps);

        routeExecution.ReleaseSteps([steps[0].RouteStepId, steps[1].RouteStepId]);

        steps[0].Released.ShouldBeTrue();
        steps[1].Released.ShouldBeTrue();
        steps[2].Released.ShouldBeFalse();
    }

    [TestMethod]
    public void ReleaseSteps_ExtendingAnAlreadyReleasedPrefix_ReturnsSuccess()
    {
        var routeExecution = CreatePlannedRouteExecution(out RouteStepExecution[] steps);
        routeExecution.ReleaseSteps([steps[0].RouteStepId]);

        var result = routeExecution.ReleaseSteps([steps[1].RouteStepId]);

        result.IsSuccess.ShouldBeTrue();
        steps[1].Released.ShouldBeTrue();
    }

    [TestMethod]
    public void ReleaseSteps_SkippingAnUnreleasedStep_ReturnsReleaseNotContiguousError()
    {
        var routeExecution = CreatePlannedRouteExecution(out RouteStepExecution[] steps);

        // Step 0 is never released, but step 1 is named — a gap in the base/horizon sequence.
        var result = routeExecution.ReleaseSteps([steps[1].RouteStepId]);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(RouteExecutionErrors.ReleaseNotContiguous);
    }

    [TestMethod]
    public void ReleaseSteps_SkippingAnUnreleasedStep_DoesNotReleaseAnything()
    {
        var routeExecution = CreatePlannedRouteExecution(out RouteStepExecution[] steps);

        routeExecution.ReleaseSteps([steps[1].RouteStepId]);

        routeExecution.Steps.ShouldAllBe(s => !s.Released);
    }

    [TestMethod]
    public void ReleaseSteps_UnknownStepId_ReturnsRouteStepNotFoundError()
    {
        var routeExecution = CreatePlannedRouteExecution(out _);

        var result = routeExecution.ReleaseSteps([Guid.NewGuid()]);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(RouteExecutionErrors.RouteStepNotFound);
    }

    [TestMethod]
    public void MintNextOrderId_FirstCall_SetsCurrentOrderId()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());

        var orderId = routeExecution.MintNextOrderId();

        routeExecution.CurrentOrderId.ShouldBe(orderId);
    }

    [TestMethod]
    public void MintNextOrderId_FirstCall_HistoryStaysEmpty()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());

        routeExecution.MintNextOrderId();

        routeExecution.OrderIdHistory.ShouldBeEmpty();
    }

    [TestMethod]
    public void MintNextOrderId_SecondCall_RecordsPreviousOrderIdInHistory()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());
        var firstOrderId = routeExecution.MintNextOrderId();

        routeExecution.MintNextOrderId();

        routeExecution.OrderIdHistory.ShouldContain(firstOrderId);
    }

    [TestMethod]
    public void MintNextOrderId_NeverReusesAPreviousOrderId()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());
        HashSet<Guid> mintedOrderIds = [];

        for (int i = 0; i < 25; i++)
        {
            mintedOrderIds.Add(routeExecution.MintNextOrderId()).ShouldBeTrue();
        }
    }

    [TestMethod]
    public void MintNextOrderId_ResetsCurrentOrderCompleted()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());
        routeExecution.MintNextOrderId();
        routeExecution.CurrentOrderCompleted = true;

        routeExecution.MintNextOrderId();

        routeExecution.CurrentOrderCompleted.ShouldBeFalse();
    }

    [TestMethod]
    public void ApplyUpdate_BeforeAnyRelease_ReplacesStepsAllUnreleased()
    {
        var routeExecution = CreatePlannedRouteExecution(out _);
        RouteStepExecution[] newSteps = [CreateStep(), CreateStep()];

        routeExecution.ApplyUpdate(newSteps);

        routeExecution.Steps.Select(s => s.RouteStepId).ShouldBe(newSteps.Select(s => s.RouteStepId));
        routeExecution.Steps.ShouldAllBe(s => !s.Released);
    }

    [TestMethod]
    public void ApplyUpdate_AfterRelease_PreservesReleasedStateForStepsThatStillExist()
    {
        var routeExecution = CreatePlannedRouteExecution(out RouteStepExecution[] steps);
        routeExecution.ReleaseSteps([steps[0].RouteStepId]);
        routeExecution.MintNextOrderId();
        RouteStepExecution[] updatedSteps = [new(steps[0].RouteStepId, steps[0].StartNodeId, steps[0].EdgeId, steps[0].EndNodeId, null), CreateStep()];

        routeExecution.ApplyUpdate(updatedSteps);

        routeExecution.Steps.Single(s => s.RouteStepId == steps[0].RouteStepId).Released.ShouldBeTrue();
    }

    [TestMethod]
    public void ApplyUpdate_AfterRelease_NewlyAddedStepsAreUnreleased()
    {
        var routeExecution = CreatePlannedRouteExecution(out RouteStepExecution[] steps);
        routeExecution.ReleaseSteps([steps[0].RouteStepId]);
        routeExecution.MintNextOrderId();
        var newStep = CreateStep();
        RouteStepExecution[] updatedSteps = [new(steps[0].RouteStepId, steps[0].StartNodeId, steps[0].EdgeId, steps[0].EndNodeId, null), newStep];

        routeExecution.ApplyUpdate(updatedSteps);

        routeExecution.Steps.Single(s => s.RouteStepId == newStep.RouteStepId).Released.ShouldBeFalse();
    }

    [TestMethod]
    public void Abort_SetsLifecycleStatusToAborted()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());

        routeExecution.Abort();

        routeExecution.LifecycleStatus.ShouldBe(RouteExecutionStatus.Aborted);
    }

    [TestMethod]
    public void Complete_SetsLifecycleStatusToCompleted()
    {
        var routeExecution = new RouteExecution(Guid.NewGuid(), Guid.NewGuid());

        routeExecution.Complete();

        routeExecution.LifecycleStatus.ShouldBe(RouteExecutionStatus.Completed);
    }

    [TestMethod]
    public void RecordStepStatus_KnownStep_UpdatesStatusAndFailureReason()
    {
        var routeExecution = CreatePlannedRouteExecution(out RouteStepExecution[] steps);

        routeExecution.RecordStepStatus(steps[0].RouteStepId, RouteStepStatus.Failed, "boom");

        steps[0].Status.ShouldBe(RouteStepStatus.Failed);
        steps[0].FailureReason.ShouldBe("boom");
    }

    [TestMethod]
    public void RecordStepStatus_UnknownStep_DoesNotThrow()
    {
        var routeExecution = CreatePlannedRouteExecution(out _);

        Should.NotThrow(() => routeExecution.RecordStepStatus(Guid.NewGuid(), RouteStepStatus.Completed));
    }
}