using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

public class ActionManagerNewTestFixture : MassTransitTestFixture<ActionManagerNew>
{
    private const string Agv2Id = "AGV002";
    public const string Agv1Id = "AGV001";
    public static Guid Agv1Guid = Agv1Id.ToGuid();
    public static Guid Agv2Guid = Agv2Id.ToGuid();

    private bool _needsCharging;
    private bool _eligibleForCharging;
    private bool _maintenanceRequested;
    private bool _isCarryingLoad;
    private bool _canGetNewLoad;
    private bool _hasRoute;

    private Mock<IChargingService> ChargingService { get; } = new();

    private Mock<IAgvLoadCapabilityService> AgvLoadCapabilityService { get; } = new();

    private Mock<IStartActionService> ActionConsumer { get; } = new();

    private Mock<IRouteService> RouteService { get; } = new();

    private AgvGraph.Node MaintenanceNode { get; set; } = null!;

    private AgvGraph.Node ChargingNode { get; }

    public Mock<IAgvRegistry> AgvRegistry { get; }

    public IAgvActionRegistry AgvActionRegistry { get; }

    public Mock<IMaintenanceService> MaintenanceService { get; } = new();

    public ActionManagerNew ActionManagerNew { get; private set; } = default!;

    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    public ActionManagerNewTestFixture()
    {
        AgvRegistry = AgvRegistryMock.GetAgvRegistry();
        var agvActionRegistryFixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActionGroups();
        agvActionRegistryFixture.InitTestData(testActions);
        AgvActionRegistry = agvActionRegistryFixture.Registry;
        ChargingNode = new AgvGraph.Node("CRG1") { Enabled = true, Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 0f };
    }

    public ActionManagerNewTestFixture WithAgvNeedsCharging(bool needsCharging = true)
    {
        _needsCharging = needsCharging;
        return this;
    }

    public ActionManagerNewTestFixture WithAgvEligibleForCharging(bool eligibleForCharging = true)
    {
        _eligibleForCharging = eligibleForCharging;
        return this;
    }

    public ActionManagerNewTestFixture WithAgvMaintenanceRequested(bool maintenanceRequested = true, AgvGraph.Node? maintenanceNode = null)
    {
        _maintenanceRequested = maintenanceRequested;
        MaintenanceNode = maintenanceNode ?? MaintenanceNode;
        return this;
    }

    public ActionManagerNewTestFixture WithAgvCarryingLoad(bool isCarryingLoad = true)
    {
        _isCarryingLoad = isCarryingLoad;
        return this;
    }

    public ActionManagerNewTestFixture WithAgvCanGetNewLoad(bool canGetNewLoad = true)
    {
        _canGetNewLoad = canGetNewLoad;
        return this;
    }

    public ActionManagerNewTestFixture WithAgvHasRouteToDestination(bool hasRoute = true)
    {
        _hasRoute = hasRoute;
        return this;
    }

    /// <summary>
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="serviceAction">The action of the service to add</param>
    /// <returns></returns>
    [UsedImplicitly]
    public ActionManagerNewTestFixture AddService(Action<IServiceCollection> serviceAction)
    {
        AddServices(serviceAction);
        return this;
    }

    /// <summary>
    /// Uses the default services
    /// </summary>
    /// <returns></returns>
    private void UseDefaultServices()
    {
        ChargingService.Setup(m => m.AgvNeedsCharging(It.IsAny<IAgv>())).Returns(_needsCharging);
        ChargingService.Setup(m => m.AgvIsEligibleForCharging(It.IsAny<IAgv>())).Returns(_eligibleForCharging);
        MaintenanceService.Setup(m => m.MaintenanceRequested(It.IsAny<IAgv>())).Returns(_maintenanceRequested);
        MaintenanceService.Setup(m => m.GetMaintenanceNode(It.IsAny<IAgv>())).Returns(MaintenanceNode).Verifiable();
        AgvLoadCapabilityService.Setup(m => m.IsCarryingLoad(It.IsAny<string>())).Returns(_isCarryingLoad);
        AgvLoadCapabilityService.Setup(m => m.CanGetNewLoad(It.IsAny<string>())).Returns(_canGetNewLoad);
        ActionConsumer.Setup(x => x.StartAction(It.IsAny<AgvAction>(), It.IsAny<CancellationToken>())).Verifiable();
        ChargingPointManager.Setup(x => x.ReserveBestCharger(It.IsAny<AgvGraph>())).Returns(ChargingNode).Verifiable();
        float? routeCosts;
        RouteService.Setup(x => x.HasRoute(It.IsAny<IAgv>(), It.IsAny<string>(), out routeCosts)).Returns(_hasRoute);
        AddServices(sc => sc.AddTransient(_ => AgvActionRegistry));
        AddServices(sc => sc.AddTransient(_ => AgvRegistry.Object));
        AddServices(sc => sc.AddTransient(_ => ChargingService.Object));
        AddServices(sc => sc.AddTransient(_ => MaintenanceService.Object));
        AddServices(sc => sc.AddTransient(_ => new Mock<ILogger<ActionManagerNew>>().Object));
        AddServices(sc => sc.AddTransient(_ => ActionConsumer.Object));
        AddServices(sc => sc.AddTransient(_ => ChargingPointManager.Object));
    }

    private ActionManagerNew GetActionManager()
    {
        return new ActionManagerNew(AgvActionRegistry,
            RouteService.Object,
            ChargingPointManager.Object,
            MaintenanceService.Object,
            new Mock<ILogger<ActionManagerNew>>().Object,
            ChargingService.Object,
            AgvRegistry.Object,
            AgvLoadCapabilityService.Object,
            ActionConsumer.Object);
    }

    public ActionManagerNewTestFixture Build()
    {
        UseDefaultServices();
        base.Build();
        ActionManagerNew = GetActionManager();
        return this;
    }

    public void VerifyActionStarted(AgvAction? expectedAction, bool compareCorrelationId = true)
    {
        expectedAction.ShouldNotBeNull();
        ActionConsumer.Verify(x => x.StartAction(It.Is<AgvAction>(a =>
            (a.CorrelationId == expectedAction.CorrelationId || !compareCorrelationId) &&
            a.AgvId == expectedAction.AgvId &&
            a.ActionType == expectedAction.ActionType &&
            a.OrderId == expectedAction.OrderId &&
            a.ParentId == expectedAction.ParentId &&
            a.TargetNodeId == expectedAction.TargetNodeId
        ), CancellationToken.None), Times.Once);
    }
}