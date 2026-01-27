using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

public abstract class ActionGroupTestFixtureBase<T> : MassTransitConsumerTestFixture<T> where T : class, IConsumer
{
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IAgvsSemaphore>>>().Object);

    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalService = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IQueueManager> _queueManager = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();

    protected Mock<IRouteService> RouteService { get; } = new();

    public Mock<IActionGroupRepository> ActionGroupRepository { get; } = new();

    protected Mock<IActionGroupDurationEstimationService> ActionGroupDurationEstimationService { get; } = new();

    protected override IEnumerable<Profile> GetProfiles() => [new ActionProfile(), new AgvProfile(), new LogEntryProfile()];

    public AgvSettingsReadModel SettingsReadModel = new();

    protected ActionGroupTestFixtureBase()
    {
        _agvDb.SetupNodeFallbacks();
        Build();
    }

    protected override MassTransitConsumerTestFixture<T> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => ActionGroupRepository.Object);
        collection.AddTransient(_ => ActionGroupDurationEstimationService.Object);
        collection.AddTransient(_ => RouteService.Object);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _actionGroupValidationService.Object);
        collection.AddTransient(_ => _actionGroupRetrievalService.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _nodeStateRepository.Object);
        collection.AddTransient(_ => _queueManager.Object);
        collection.AddTransient(_ => _logEntryRepository.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient<ActionCommandHelper>();
        ActionGroupRepository.Setup(x => x.IsActionGroupQueued(It.IsAny<Guid>())).ReturnsAsync(true);
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetRequiredStationBaseInformation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        return this;
    }

    public void VerifyGetNextManualActionGroupForAgvAsyncInput(Guid agvCorrelationId, List<ActionType>? allowedActionTypes = null)
    {
        if (allowedActionTypes == null)
        {
            ActionGroupRepository.Verify(x => x.GetNextManualActionGroupForAgvAsync(
                    It.Is<Guid>(g => g == agvCorrelationId),
                    It.Is<List<ActionType>?>(a => a == null)),
                Times.Once);
            return;
        }
        ActionGroupRepository.Verify(x => x.GetNextManualActionGroupForAgvAsync(
                It.Is<Guid>(g => g == agvCorrelationId),
                It.Is<List<ActionType>>(a => a.SequenceEqual(allowedActionTypes))),
            Times.Once);
    }

    protected ActionGroupTestFixtureBase(TimeSpan? inactivityTimeout = null)
    {
        _agvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(SettingsReadModel);

        Build();
        if (inactivityTimeout != null)
        {
            TestHarness.TestInactivityTimeout = (TimeSpan)inactivityTimeout;
        }
        ActionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?)null);
        ActionGroupRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<ActionGroup>())).Verifiable();
        ActionGroupRepository.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).Verifiable();
    }

    public ActionGroupTestFixtureBase<T> WithAgvsAtQueueHead(ICollection<Guid> agvs)
    {
        _queueManager.Setup(x => x.GetAgvsAtQueueHeads()).ReturnsAsync(agvs);
        foreach (var agv in agvs)
        {
            _agvDb.AddAgvData(AgvDataGenerator.GenerateAgvData(correlationId: agv));
        }
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithAgvsWithPriorityActionGroups(ICollection<Guid> agvs)
    {
        ActionGroupRepository.Setup(x => x.FilterAgvsWithPriorityActionGroups(It.IsAny<ICollection<Guid>>())).ReturnsAsync(agvs);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithClosestAgv(Guid? closestAgv, Route? route)
    {
        RouteService.SetupSequence(x => x.GetAgvRouteClosestToTarget(It.IsAny<ICollection<(TransportOrder.Domain.Models.Agv.Agv agv, AgvData? agvData)>>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync((closestAgv, route));
        ActionGroupDurationEstimationService.Setup(x => x.AddNonRouteCosts(It.IsAny<bool>(), It.IsAny<ActionGroup>(), It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(42);
        return this;
    }

    public ActionGroupTestFixtureBase<T> AddAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvData(AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId));
        _agvDb.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        return this;
    }

    public Task<TransportOrder.Domain.Models.Agv.Agv> GetAgvAsync(Guid agvCorrelationId)
    {
        return _agvDb.GetAgvAsync(agvCorrelationId);
    }

    public ActionGroupTestFixtureBase<T> AddAgvs(ICollection<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _queueManager.Setup(x => x.GetAgvsAtQueueHeads()).ReturnsAsync(agvs.Select(a => a.CorrelationId).ToList());
        foreach (var agv in agvs)
        {
            _agvDb.AddAgvData(AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId));
        }
        _agvDb.AddAgvs(agvs.ToList());
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithAddActionGroupIsSuccessful(bool isSuccessful = true)
    {
        ActionGroupRepository.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).ReturnsAsync(isSuccessful);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithAddOrUpdateActionGroupIsUpdating(bool isUpdating = true)
    {
        ActionGroupRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<ActionGroup>())).ReturnsAsync(isUpdating);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithRunningIdleActionGroups(ICollection<ActionGroup> actionGroups)
    {
        ActionGroupRepository.Setup(x => x.GetRunningIdleActionGroups()).ReturnsAsync(actionGroups);
        ActionGroupRepository.Setup(x => x.GetAgvsMovingCargo(It.IsAny<ICollection<Guid>>())).ReturnsAsync(new List<Guid>());

        WithNode(actionGroups.SelectMany(ag => ag.Actions.Select(a => a.Target.Node)).ToArray());

        return this;
    }

    public ActionGroupTestFixtureBase<T> WithRunningChargeActionGroups(ICollection<ActionGroup> actionGroups)
    {
        ActionGroupRepository.Setup(x => x.GetRunningChargeActionGroups()).ReturnsAsync(actionGroups);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithActionGroupToSaveAbortion(ActionGroup actionGroup)
    {
        ActionGroupRepository.Setup(x => x.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        return this;
    }

    public virtual ActionGroupTestFixtureBase<T> AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction = true, bool hasNotStartedAction = true, bool existsPriorToTest = true, bool isManual = false)
    {
        ActionGroupRepository.Setup(x => x.UpdateAsync(It.IsAny<ActionGroup>())).Verifiable();
        ActionGroupRepository.Setup(x => x.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<ActionState>(), It.IsAny<Reason>(), It.IsAny<DateTimeOffset>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.GetSubsequentActionAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Created) ?? actionGroup.Actions.First());
        ActionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<ActionGroupState>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>())).ReturnsAsync([actionGroup]);
        if (existsPriorToTest)
        {
            ActionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync([actionGroup]);
            _chooseAgvActionGroupService.Setup(x => x.GetNotStartedActionGroupAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(actionGroup);
            ActionGroupRepository.Setup(x => x.GetStartedActionGroupForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
            ActionGroupRepository.Setup(x => x.GetCreatedAndStartedActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync([actionGroup]);
            ActionGroupRepository.Setup(x => x.GetNextManualActionGroupForAgvAsync(It.IsAny<Guid>(), It.IsAny<List<ActionType>?>())).ReturnsAsync(actionGroup);
        }
        else
        {
            ActionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync([]);
            _chooseAgvActionGroupService.Setup(x => x.GetNotStartedActionGroupAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(null as ActionGroup);
            ActionGroupRepository.Setup(x => x.GetStartedActionGroupForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(null as ActionGroup);
            ActionGroupRepository.Setup(x => x.GetCreatedAndStartedActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync([]);
            ActionGroupRepository.Setup(x => x.GetNextManualActionGroupForAgvAsync(It.IsAny<Guid>(), It.IsAny<List<ActionType>?>())).ReturnsAsync((ActionGroup?)null);
        }
        ActionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.HasNotStartedActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasNotStartedAction);
        ActionGroupRepository.Setup(x => x.HasUnfinishedDropActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasUnfinishedAction);
        ActionGroupRepository.Setup(x => x.GetRequiredActionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());

        if (isManual)
        {
            ActionGroupRepository.Setup(x => x.GetNextManualActionGroupForAgvAsync(It.IsAny<Guid>(), It.IsAny<List<ActionType>>())).ReturnsAsync(actionGroup);
        }
        else
        {
            ActionGroupRepository.Setup(x => x.GetNextManualActionGroupForAgvAsync(It.IsAny<Guid>(), It.IsAny<List<ActionType>>())).ReturnsAsync((ActionGroup?)null);
        }

        WithNode(actionGroup.Actions.Select(a => a.Target.Node).ToArray());

        return this;
    }

    public ActionGroupTestFixtureBase<T> WithCurrentActionGroupsToTarget(ICollection<ActionGroup> actionGroups)
    {
        ActionGroupRepository.Setup(x => x.GetCurrentActionGroupsToTarget(It.IsAny<Guid>())).ReturnsAsync(actionGroups);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithNextPickAtNode(ActionGroup? actionGroup)
    {
        ActionGroupRepository.Setup(x => x.GetNextPickAtNodeAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithReadyAgvs(ICollection<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDb.Setup(x => x.Agvs.GetReadyAgvsWithAtLeastLowBatteryLevel()).ReturnsAsync(agvs);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithRoute(ICollection<Guid> nodes)
    {
        RouteService.Setup(r => r.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<Guid>(), null, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Route(12, nodes.Select(n => new GraphNode(n, "name")).ToArray(), []));

        RouteService.Setup(r => r.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Route(12, nodes.Select(n => new GraphNode(n, "name")).ToArray(), []));

        return this;
    }

    public ActionGroupTestFixtureBase<T> WithQueuesOnRoute(ICollection<TransportOrder.Domain.Models.Queue.Queue> queues)
    {
        _queueManager.Setup(x => x.GetQueuesOnRoute(It.IsAny<ICollection<Guid>>())).ReturnsAsync(queues);
        return this;
    }

    public ActionGroupTestFixtureBase<T> AddActionGroupRetrieval(bool headingTo)
    {
        _actionGroupRetrievalService.Setup(x => x.IsAgvHeadingToNodeOfType(It.IsAny<Guid>(), It.IsAny<AgvGraph.NodeType>())).ReturnsAsync(headingTo);
        return this;
    }

    public ActionGroupTestFixtureBase<T> AddActionGroupTargetNodeType(ActionGroup actionGroup, AgvGraph.NodeType nodeType)
    {
        _actionGroupRetrievalService.Setup(x => x.EnrichActionGroupWithNodesAsync(It.IsAny<ActionGroup>())).ReturnsAsync(actionGroup.ConvertFromActionGroupWithGivenNodeType(nodeType)!);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithLastActionDockFront(bool dockFront)
    {
        ActionGroupRepository.Setup(x => x.GetDockFrontOfLastCompletedAction(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(dockFront);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithHasCargo(bool hasCargo)
    {
        _agvDb.Setup(x => x.Cargos.HasAgvCargoAsync(It.IsAny<Guid>())).ReturnsAsync(hasCargo);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithLastActionIsMoveToCoordinate(bool isMoveToCoordinate)
    {
        ActionGroupRepository.Setup(x => x.IsLastCompletedActionMoveToCoordinate(It.IsAny<Guid>())).ReturnsAsync(isMoveToCoordinate);
        return this;
    }

    public ActionGroupTestFixtureBase<T> AddRoute(string[] nodes, Guid[] edges)
    {
        _agvDb.AddAgvData((AgvData?)null);
        RouteService.WithRoute(0f, nodes, edges);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithLockedStations()
    {
        _stationRepository.Setup(a => a.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station("", AgvGraph.NodeType.Charge, []) { LockedBy = "lock" });
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithErrors(IList<AgvError> errors)
    {
        _agvDb.AddActiveErrors(errors);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithNode(params GraphNode?[] nodes)
    {
        _agvDb.AddNode(nodes);
        return this;
    }

    public ActionGroupTestFixtureBase<T> WithNodeState(NodeLock? nodeLock)
    {
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<Guid>())).ReturnsAsync(nodeLock);
        return this;
    }

    public void SetupValidationResult(ValidationResult validationResult)
    {
        _actionGroupValidationService.Setup(x => x.ValidateAsync(It.IsAny<ActionGroup>())).ReturnsAsync(validationResult);
    }

    public void IsActionGroupSaved(Times howOften)
    {
        ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<ActionGroup>()), howOften);
        ActionGroupRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<ActionGroup>()), howOften);
    }

    public async Task<Response<TResult>> GetResponseAsync<TResult, TRequest>(TRequest request)
        where TResult : class
        where TRequest : class
    {
        IRequestClient<TRequest> client = TestHarness.Bus!.CreateRequestClient<TRequest>();
        Response<TResult> response = await client.GetResponse<TResult>(request);
        return response;
    }

    public Common.Messages.ActionGroups.ActionGroup ActionGroupAsMessage(ActionGroup actionGroup)
    {
        return Mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
    }

    public ActionGroup ActionGroupAsDomainObject(Common.Messages.ActionGroups.ActionGroup actionGroup)
    {
        return Mapper.Map<ActionGroup>(actionGroup);
    }
}