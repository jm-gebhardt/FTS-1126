using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

[TestClass]
public class ActionManagerNewTest
{
    [TestMethod]
    public async Task AssignAsync_WhenAgvIsCarryingLoadAndCanGetNewLoadAndNeedsCharging_StartSubsequentActionToBeUnloadedInNextCycleForCharging()
    {
        var fixture = new ActionManagerNewTestFixture()
            .WithAgvCarryingLoad()
            .WithAgvCanGetNewLoad()
            .WithAgvNeedsCharging()
            .Build();
        var agv = fixture.AgvRegistry.Object.GetAgv(ActionManagerNewTestFixture.Agv1Guid)!;
        var expectedAction = fixture.AgvActionRegistry.GetSubsequentAction(ActionManagerNewTestFixture.Agv1Id);

        await fixture.ActionManagerNew.AssignAsync(agv, CancellationToken.None);
        fixture.VerifyActionStarted(expectedAction);
    }

    [TestMethod]
    public async Task AssignAsync_WhenAgvIsCarryingLoadAndCanGetNewLoad_StartSubsequentActionToWorkOffItsActions()
    {
        var fixture = new ActionManagerNewTestFixture()
            .WithAgvCarryingLoad()
            .WithAgvCanGetNewLoad()
            .WithAgvHasRouteToDestination()
            .Build();
        var actionManager = fixture.ActionManagerNew;
        var agv = fixture.AgvRegistry.Object.GetAgv(ActionManagerNewTestFixture.Agv1Guid);
        agv.ShouldNotBeNull();
        var expectedAction = fixture.AgvActionRegistry.GetSubsequentAction(ActionManagerNewTestFixture.Agv1Id);

        await actionManager.AssignAsync(agv, CancellationToken.None);
        fixture.VerifyActionStarted(expectedAction);
    }

    [TestMethod]
    public async Task AssignAsync_WhenAgvIsCarryingLoadAndCannotGetNewLoad_StartSubsequentActionToWorkOffItsActions()
    {
        var fixture = new ActionManagerNewTestFixture()
            .WithAgvCarryingLoad()
            .Build();
        var agv = fixture.AgvRegistry.Object.GetAgv(ActionManagerNewTestFixture.Agv1Guid);
        agv.ShouldNotBeNull();

        var expectedAction = fixture.AgvActionRegistry.GetSubsequentAction(ActionManagerNewTestFixture.Agv1Id);

        await fixture.ActionManagerNew.AssignAsync(agv, CancellationToken.None);
        fixture.VerifyActionStarted(expectedAction);
    }

    [TestMethod]
    public async Task AssignAsync_WhenAgvIsNotCarryingLoadAndNeedsCharging_StartChargingAction()
    {
        var fixture = new ActionManagerNewTestFixture()
            .WithAgvNeedsCharging()
            .Build();
        var agv = fixture.AgvRegistry.Object.GetAgv(ActionManagerNewTestFixture.Agv1Guid);
        agv.ShouldNotBeNull();
        var expectedAction = new AgvAction(
            Guid.Empty,
            agv.AgvId,
            ActionType.Charge,
            string.Empty,
            Guid.Empty,
            "CRG1");

        await fixture.ActionManagerNew.AssignAsync(agv, CancellationToken.None);
        fixture.ChargingPointManager.Verify(x => x.ReserveBestCharger(It.IsAny<AgvGraph>()), Times.Once);
        fixture.VerifyActionStarted(expectedAction, false);
    }

    [TestMethod]
    public async Task AssignAsync_WhenAgvIsNotCarryingLoad_StartNextParentedByAction()
    {
        var fixture = new ActionManagerNewTestFixture()
            .WithAgvHasRouteToDestination()
            .Build();
        var actionManager = fixture.ActionManagerNew;
        var agv = fixture.AgvRegistry.Object.GetAgv(ActionManagerNewTestFixture.Agv1Guid);
        agv.ShouldNotBeNull();
        var expectedAction = actionManager.GetNextParentedByAction(agv);
        expectedAction.ShouldNotBeNull();

        await actionManager.AssignAsync(agv, CancellationToken.None);
        fixture.VerifyActionStarted(expectedAction);
    }

    [TestMethod]
    public async Task AssignAsync_WhenAgvNeedsMaintenance_StartMaintenanceAction()
    {
        var maintenanceNode = new AgvGraph.Node("MNT1") { Enabled = true, Type = AgvGraph.NodeType.Park, X = 0f, Y = 0f, Z = 0f };
        var fixture = new ActionManagerNewTestFixture()
            .WithAgvMaintenanceRequested(maintenanceNode: maintenanceNode)
            .Build();

        var agv = fixture.AgvRegistry.Object.GetAgv(ActionManagerNewTestFixture.Agv1Guid);
        agv.ShouldNotBeNull();
        var expectedAction = new AgvAction(
            Guid.Empty,
            agv.AgvId,
            ActionType.Move,
            string.Empty,
            Guid.Empty,
            maintenanceNode.Id);

        await fixture.ActionManagerNew.AssignAsync(agv, CancellationToken.None);
        fixture.MaintenanceService.Verify(x => x.GetMaintenanceNode(It.IsAny<IAgv>()), Times.Once);
        fixture.VerifyActionStarted(expectedAction, false);
    }

    [TestMethod]
    public async Task AssignAsync_WhenThereIsNoSubsequentActionAndAgvIsEligibleForCharging_StartChargingAction()
    {
        var fixture = new ActionManagerNewTestFixture()
            .WithAgvEligibleForCharging()
            .Build();

        var actionManager = fixture.ActionManagerNew;
        var agv = fixture.AgvRegistry.Object.GetAgv(ActionManagerNewTestFixture.Agv2Guid);
        agv.ShouldNotBeNull();
        var expectedAction = new AgvAction(
            Guid.Empty,
            agv.AgvId,
            ActionType.Charge,
            string.Empty,
            Guid.Empty,
            "CRG1");

        await actionManager.AssignAsync(agv, CancellationToken.None);
        fixture.ChargingPointManager.Verify(x => x.ReserveBestCharger(It.IsAny<AgvGraph>()), Times.Once);
        fixture.VerifyActionStarted(expectedAction, false);
    }

    [TestMethod]
    public async Task AssignAsync_WhenThereIsNoSubsequentActionAndAgvIsNotEligibleForCharging_StartParkAction()
    {
        var fixture = new ActionManagerNewTestFixture()
            .Build();

        var actionManager = fixture.ActionManagerNew;
        var agv = fixture.AgvRegistry.Object.GetAgv(ActionManagerNewTestFixture.Agv2Guid);
        agv.ShouldNotBeNull();
        var expectedAction = new AgvAction(
            Guid.Empty,
            agv.AgvId,
            ActionType.Move,
            string.Empty,
            Guid.Empty,
            "parkTestId");

        await actionManager.AssignAsync(agv, CancellationToken.None);
        fixture.VerifyActionStarted(expectedAction, false);
    }
}