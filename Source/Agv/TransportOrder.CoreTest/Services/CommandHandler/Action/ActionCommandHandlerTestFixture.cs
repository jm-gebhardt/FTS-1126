using AutoMapper;
using FluentValidation.Results;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

public class ActionCommandHandlerTestFixture : MassTransitConsumerTestFixture<ActionCommandHandler>
{
    private readonly Mock<IRouteService> _routeService = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly Mock<IChargingPointManager> _chargingPointManager = new();
    private readonly Mock<IParkPointService> _parkPointService = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();
    private readonly Mock<ISettingsProvider<AgvSettings>> _settingsProvider = new();
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalService = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepositoryMock = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _routeService.Object);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddTransient(_ => _maintenanceService.Object);
        collection.AddTransient(_ => _chargingPointManager.Object);
        collection.AddTransient(_ => _parkPointService.Object);
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _agvRepository.Object);
        collection.AddTransient(_ => _actionGroupValidationService.Object);
        collection.AddTransient(_ => _settingsProvider.Object);
        collection.AddTransient(_ => _actionGroupRetrievalService.Object);
        collection.AddTransient(_ => _nodeStateRepositoryMock.Object);
    }

    public ActionCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        params Action<IServiceCollection>[] services) : base(busRegistrationConfigurator, inMemoryConfiguration, services)
    {
        _settingsProvider.Setup(s => s.GetSettings()).Returns(new AgvSettings()
        {
            BatteryLevelCritical = 10,
            BatteryLevelGood = 80,
            BatteryLevelFull = 100,
            BatteryLevelLow = 20,
            AgvOfflineTimeoutMsWhileBusy = 10000,
            AgvOfflineTimeoutMsWhileIdle = 10000
        });
        Build();
        if (inactivityTimeout != null)
        {
            TestHarness!.TestInactivityTimeout = (TimeSpan) inactivityTimeout;
        }
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?) null);
        _actionGroupRepository.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).Verifiable();
    }

    public ActionCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null)
    {
        _settingsProvider.Setup(s => s.GetSettings()).Returns(new AgvSettings()
        {
            BatteryLevelCritical = 10,
            BatteryLevelGood = 80,
            BatteryLevelFull = 100,
            BatteryLevelLow = 20,
            AgvOfflineTimeoutMsWhileBusy = 10000,
            AgvOfflineTimeoutMsWhileIdle = 10000
        });
        Build();
        if (inactivityTimeout != null)
        {
            TestHarness!.TestInactivityTimeout = (TimeSpan) inactivityTimeout;
        }
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?) null);
        _actionGroupRepository.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).Verifiable();
    }

    public void AddAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvRepository.AddAgvsToMockRepository(new List<TransportOrder.Domain.Models.Agv.Agv> {agv});
        _agvRepository.AddAgvsToMockRepository(new List<TransportOrder.Domain.Models.Agv.Agv> {agv});
    }

    public void AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction = true, bool hasNotStartedAction = true, bool existsPriorToTest = true)
    {
        _actionGroupRepository.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<ActionState>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.GetSubsequentActionAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<ActionGroupState>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>())).ReturnsAsync(new List<ActionGroup> {actionGroup});
        if (existsPriorToTest)
        {
            _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(new List<ActionGroup> {actionGroup});
        }
        else
        {
            _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(new List<ActionGroup>());
        }
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.HasNotStartedActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasNotStartedAction);
        _actionGroupRepository.Setup(x => x.HasUnfinishedDropActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasUnfinishedAction);
        _actionGroupRepository.Setup(x => x.GetRequiredActionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());
    }

    public void AddActionGroupRetrieval(bool headingTo)
    {
        _actionGroupRetrievalService.Setup(x => x.IsAgvHeadingToNodeOfType(It.IsAny<Guid>(), It.IsAny<AgvGraph.NodeType>())).ReturnsAsync(headingTo);
    }

    public void AddActionGroupTargetNodeType(ActionGroup actionGroup, AgvGraph.NodeType nodeType)
    {
        _actionGroupRetrievalService.Setup(x => x.EnrichActionGroupWithNodesAsync(It.IsAny<ActionGroup>())).ReturnsAsync(actionGroup.ConvertFromActionGroupWithGivenNodeType(nodeType)!);
    }

    public void AddChargingPointManager(GraphNode chargeGraphNode)
    {
        _chargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(chargeGraphNode);
        _nodeStateRepositoryMock.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(new NodeLock(chargeGraphNode.Id, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
    }

    public void AddParkingPointManager(GraphNode parkGraphNode)
    {
        _parkPointService.Setup(x => x.ReserveParkAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(parkGraphNode);
        _nodeStateRepositoryMock.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(new NodeLock(parkGraphNode.Id, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
    }

    public void AddRoute(string[] nodes)
    {
        _routeService.Setup(x => x.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(new Route(1f, nodes));
    }

    public void SetupValidationResult(List<ValidationFailure> failures)
    {
        _actionGroupValidationService.Setup(x => x.ValidateAsync(It.IsAny<ActionGroup>())).ReturnsAsync(failures);
    }

    public void IsActionGroupSaved(Times howOften)
    {
        _actionGroupRepository.Verify(x => x.AddAsync(It.IsAny<ActionGroup>()), howOften);
    }

    public static IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(new ActionGroupProfile(), new AgvProfile());
        return mapper;
    }

    public async Task<Response<TResult>> GetResponseAsync<TResult, TRequest>(TRequest request)
        where TResult : class
        where TRequest : class
    {
        IRequestClient<TRequest> client = TestHarness!.Bus!.CreateRequestClient<TRequest>();
        Response<TResult> response = await client.GetResponse<TResult>(request);
        return response;
    }
}