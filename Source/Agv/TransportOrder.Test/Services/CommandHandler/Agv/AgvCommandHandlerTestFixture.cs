using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language;
using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using Cargo = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Cargo;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Agv;

public class AgvCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvCommandHandler>
{
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IAgvsSemaphore>>>().Object);
    private readonly SemaphoreProvider<IQueueingSemaphore> _queueSemaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IQueueingSemaphore>>>().Object);
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<ILogger<AgvCommandHandler>> _logger = new();
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly SystemOptions _productionOptions = new() { AutoAccept = true };
    private readonly Mock<IChargingPointManager> _chargingPointManager = new();
    private readonly Mock<IQueueManager> _queueManager = new();
    public readonly Mock<IAgvDb> AgvDb = new();
    private readonly Mock<IParkPointService> _parkPointService = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepositoryMock = new();
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalService = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IRouteService> _routeService = new();
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    private static IEnumerable<Profile> Profiles => [new AgvStatusProfile(), new AgvProfile(), new CargoProfile(), new ActionProfile(), new LogEntryProfile()];

    protected override IEnumerable<Profile> GetProfiles() => Profiles;

    public AgvCommandHandlerTestFixture()
    {
        AgvDb.SetupNodeFallbacks();
        _nodeStateRepositoryMock.Setup(x => x.GetNodeLocksForAgv(It.IsAny<Guid>())).ReturnsAsync([]);

        Build();
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
    }

    protected override MassTransitConsumerTestFixture<AgvCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
        collection.AddSingleton<ISemaphoreProvider<IQueueingSemaphore>>(_ => _queueSemaphoreProvider);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _logger.Object);
        collection.AddTransient(_ => _mapRepository.Object);
        collection.AddTransient(_ => _productionOptions);
        collection.AddTransient(_ => AgvDb.Object);
        collection.AddTransient(_ => _chargingPointManager.Object);
        collection.AddTransient(_ => _parkPointService.Object);
        collection.AddTransient(_ => _queueManager.Object);
        collection.AddTransient(_ => _queueRepository.Object);
        collection.AddTransient(_ => _nodeStateRepositoryMock.Object);
        collection.AddTransient(_ => _actionGroupRetrievalService.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _routeService.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient(_ => _logEntryRepository.Object);
        _chargingPointManager.Setup(x => x.IsOnAChargingNodeAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).Verifiable();
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        return this;
    }

    public void VerifyAgvUpdated(Times times)
    {
        AgvDb.Verify(x => x.Agvs.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), times);
    }

    public AgvCommandHandlerTestFixture WithInfiniteCharging(bool infiniteCharging)
    {
        var agvSettings = new AgvSettingsReadModel(15000, 20000, 5, 40, 60, 80, infiniteCharging);
        AgvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(agvSettings);
        return this;
    }

    public AgvCommandHandlerTestFixture WithAgvsAtQueueHead(ICollection<Guid> agvs)
    {
        _queueManager.Setup(x => x.GetAgvsAtQueueHeads()).ReturnsAsync(agvs);
        return this;
    }

    public AgvCommandHandlerTestFixture WithAgvsToMove(Dictionary<Guid, Guid>? agvsToMove, int? queueSize = null, string? nodeName = null, bool swapped = false)
    {
        _queueManager.Setup(x => x.TryDequeueAgv(It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(agvsToMove);
        _queueManager.Setup(x => x.TryEscapeQueue(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>())).ReturnsAsync((agvsToMove, queueSize));
        _queueManager.Setup(x => x.TrySwapAgv(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync((agvsToMove, nodeName?.ToGuid(), swapped));
        return this;
    }

    public AgvCommandHandlerTestFixture WithQueueInternalMoves(ICollection<Guid> actionGroups)
    {
        _queueManager.Setup(x => x.GetQueueInternalActionGroups(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<Guid?>())).ReturnsAsync(actionGroups);
        return this;
    }

    public AgvCommandHandlerTestFixture WithEvacuationMoves(ICollection<Guid> actionGroups)
    {
        _queueManager.Setup(x => x.GetQueueEvacuationMoves(It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(actionGroups);
        return this;
    }

    public AgvCommandHandlerTestFixture WithIsOnAQueueNode(bool isOnAQueueNode)
    {
        _queueManager.Setup(x => x.IsOnAQueueNode(It.IsAny<Guid>())).ReturnsAsync(isOnAQueueNode);
        return this;
    }

    public AgvCommandHandlerTestFixture WithClosestAgv(Guid? closestAgv)
    {
        _routeService.SetupSequence(x => x.GetAgvRouteClosestToTarget(It.IsAny<ICollection<(TransportOrder.Domain.Models.Agv.Agv agv, AgvData? agvData)>>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync((closestAgv, new Route(42, [], [])));
        return this;
    }

    public AgvCommandHandlerTestFixture WithZones(ICollection<TransportOrder.Domain.Models.Zone.Zone> zones)
    {
        AgvDb.AddZones(zones);
        return this;
    }

    public AgvCommandHandlerTestFixture WithPostion(AgvPositionStatus position)
    {
        AgvDb.AddPosition(position);
        return this;
    }

    public AgvCommandHandlerTestFixture WithEnqueueingTarget(string? queueNode)
    {
        _queueManager.Setup(x => x.TryEnqueueAgv(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(queueNode?.ToGuid());
        AgvDb.AddNode(queueNode?.ToGraphNode());
        return this;
    }

    public AgvCommandHandlerTestFixture AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        AgvDb.AddAgvs(agvs);
        AgvDb.Setup(r => r.Agvs.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()))
            .ReturnsAsync((TransportOrder.Domain.Models.Agv.Agv a) => agvs.Any(agv => agv.CorrelationId == a.CorrelationId))
            .Verifiable();
        AgvDb.Setup(r => r.Agvs.AddAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).Verifiable();

        AgvDb.AddNode(agvs.Select(a => a.AgvStatus?.LastNodeId?.ToGraphNode()).ToArray());
        return this;
    }

    [PublicAPI]
    public AgvCommandHandlerTestFixture SetupAgv(bool hasPendingActions = false, bool exists = true)
    {
        _actionGroupRepository.Setup(x =>
            x.AgvHasPendingActions(It.IsAny<Guid>())).ReturnsAsync(hasPendingActions);
        AgvDb.AddAgvGraph(graphExists: exists);

        return this;
    }

    [PublicAPI]
    public AgvCommandHandlerTestFixture WithCurrentAgvAction(AgvAction action)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(action);
        return this;
    }

    [PublicAPI]
    public AgvCommandHandlerTestFixture WithCurrentAssignedActionGroup(bool hasAssignedActionGroup)
    {
        _actionGroupRepository.Setup(x => x.HasCurrentAssignedActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(hasAssignedActionGroup);
        return this;
    }

    public AgvCommandHandlerTestFixture WithCurrentStartedOrCreatedActionGroups(IReadOnlyList<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetCreatedAndStartedActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups);
        return this;
    }

    public AgvCommandHandlerTestFixture WithHasStartedAction(bool hasStartedAction)
    {
        _actionGroupRepository.Setup(x => x.HasStartedActionAsync(It.IsAny<Guid>())).ReturnsAsync(hasStartedAction);
        return this;
    }

    public AgvCommandHandlerTestFixture WithSetCargoReturnValue(Guid agvCorrelationId, IReadOnlyCollection<Cargo> cargos, bool returnValue)
    {
        AgvDb.Setup(x => x.Cargos.SetCargoAsync(agvCorrelationId, cargos)).ReturnsAsync(returnValue);
        return this;
    }

    public AgvCommandHandlerTestFixture WithHasCargoStates(params bool[] hasCargos)
    {
        ISetupSequentialResult<Task<bool>> setup = AgvDb.SetupSequence(x => x.Cargos.HasAgvCargoAsync(It.IsAny<Guid>()));
        foreach (bool hasCargo in hasCargos)
        {
            setup.ReturnsAsync(hasCargo);
        }
        return this;
    }

    public AgvCommandHandlerTestFixture WithGraphExists(bool exists)
    {
        AgvDb.AddAgvGraph(graphExists: exists);
        return this;
    }

    public AgvCommandHandlerTestFixture WithNode(params GraphNode?[] nodes)
    {
        AgvDb.AddNode(nodes);

        return this;
    }

    public AgvCommandHandlerTestFixture WithoutAgv()
    {
        AgvDb.AddAgvs();
        return this;
    }

    public AgvCommandHandlerTestFixture WithAgvIsOnCharger(bool isAgvOnCharger)
    {
        _chargingPointManager.Setup(x => x.IsOnAChargingNodeAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(isAgvOnCharger);
        return this;
    }

    public AgvCommandHandlerTestFixture WithActiveErrors(IList<AgvError> errors)
    {
        AgvDb.AddActiveErrors(errors);
        return this;
    }

    public AgvCommandHandlerTestFixture WithHasOnlineAgvs(bool hasOnlineAgvs = true)
    {
        AgvDb.Setup(r => r.Agvs.HasOnlineAgvs()).ReturnsAsync(hasOnlineAgvs);
        return this;
    }

    public AgvCommandHandlerTestFixture AddParkingPointManager(GraphNode? parkGraphNode)
    {
        _parkPointService.Setup(x => x.HasParkersAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        _parkPointService.Setup(x => x.ReserveParkAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()))
            .ReturnsAsync(() =>
            {
                if (parkGraphNode == null)
                {
                    throw new ParkNodeNotAvailableException(Guid.Empty);
                }
                return parkGraphNode;
            });
        if (parkGraphNode != null)
        {
            _nodeStateRepositoryMock.Setup(x => x.GetNodeStateAsync(It.IsAny<Guid>())).ReturnsAsync(new NodeLock(parkGraphNode, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
        }
        return this;
    }

    public AgvCommandHandlerTestFixture HasAlreadyReservedNode(bool hasNodeReserved)
    {
        _queueManager.Setup(x => x.IsOnAQueueNode(It.IsAny<Guid>())).ReturnsAsync(false);
        _parkPointService.Setup(x => x.IsOnAParkNodeAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(hasNodeReserved);
        _parkPointService.Setup(x => x.AgvHasReservedParkingNodeAsync(It.IsAny<Guid>())).ReturnsAsync(hasNodeReserved);
        _chargingPointManager.Setup(x => x.AgvHasReservedChargerAsync(It.IsAny<Guid>())).ReturnsAsync(hasNodeReserved);
        return this;
    }

    public AgvCommandHandlerTestFixture AddChargingPointManager(GraphNode? chargeGraphNode)
    {
        _chargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(),
                null))
            .ReturnsAsync(() =>
            {
                if (chargeGraphNode == null)
                {
                    throw new NoChargeStationAvailableException();
                }
                return chargeGraphNode;
            });
        if (chargeGraphNode != null)
        {
            _nodeStateRepositoryMock.Setup(x => x.GetNodeStateAsync(It.IsAny<Guid>())).ReturnsAsync(new NodeLock(chargeGraphNode, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
        }
        return this;
    }

    public AgvCommandHandlerTestFixture AddChargingNode(bool isOnCharger, GraphNode? chargeGraphNode)
    {
        _chargingPointManager.Setup(x => x.IsOnAChargingNodeAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(isOnCharger);
        _chargingPointManager.Setup(x => x.GetReservedChargerForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(chargeGraphNode != null ? new NodeLock(chargeGraphNode, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow) : null);
        _chargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), null)).ReturnsAsync(() => chargeGraphNode ?? throw new NoChargeStationAvailableException());
        return this;
    }

    public AgvCommandHandlerTestFixture AddActionGroupRetrieval(bool headingTo)
    {
        _actionGroupRetrievalService.Setup(x => x.IsAgvHeadingToNodeOfType(It.IsAny<Guid>(), It.IsAny<AgvGraph.NodeType>())).ReturnsAsync(headingTo);
        return this;
    }

    public AgvCommandHandlerTestFixture WithUnfinishedActionGroup(bool hasUnfinishedGroup)
    {
        _actionGroupRepository.Setup(x => x.HasUnfinishedActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(hasUnfinishedGroup);
        return this;
    }

    public AgvCommandHandlerTestFixture WithActionGroup(ActionGroup? actionGroup)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);

        if (actionGroup != null)
        {
            WithNode(actionGroup.Actions.Select(a => a.Target.Node).ToArray());
        }
        return this;
    }

    public AgvCommandHandlerTestFixture WithActionGroups(List<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(),
                It.IsAny<SortBy>(), It.IsAny<bool>()))
            .ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetCreatedAndStartedActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups);

        return this;
    }

    public AgvCommandHandlerTestFixture WithAgvsGraph(bool withParkingNodes = true)
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        ICollection<GraphNode> nodes = withParkingNodes ? graph.Nodes : graph.Nodes.Where(n => !n.IsParker()).ToList();

        // Expression "node => node.IsParker()"
        AgvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>?>()))
            .ReturnsAsync(nodes.Where(n => n.IsParker()));
        _parkPointService.Setup(x => x.HasParkersAsync(It.IsAny<Guid>())).ReturnsAsync(nodes.Any(n => n.IsParker()));
        return this;
    }

    public AgvCommandHandlerTestFixture WithNodesStates(ICollection<NodeLock> nodeLocks, ICollection<GraphNode> nodes, bool nodeReservationFails = false, string? lastNodeId = null)
    {
        _nodeStateRepositoryMock.Setup(x => x.GetNodeLocksForAgv(It.IsAny<Guid>())).ReturnsAsync(nodeLocks);
        AgvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Expression<Func<GraphNode, bool>>?>())).ReturnsAsync(nodes);
        _nodeStateRepositoryMock.Setup(x => x.ReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(nodeReservationFails);
        _nodeStateRepositoryMock.Setup(x => x.ReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Verifiable();
        _nodeStateRepositoryMock.Setup(x => x.ForceOccupyNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Verifiable();
        _nodeStateRepositoryMock.Setup(x => x.ForceReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Verifiable();
        _nodeStateRepositoryMock.Setup(x => x.ForceReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);
        AgvDb.Setup(x => x.Graphs.TryGetNodeByIdAsync(It.IsAny<Guid>())).ReturnsAsync(lastNodeId == null ? null : nodes.FirstOrDefault(n => n.Id == lastNodeId.ToGuid()));

        AgvDb.AddNode(nodeLocks.Select(n => n.Node).Concat(nodes).ToArray());
        return this;
    }

    public void VerifyNodeStateChange(Times forceReserve, Times forceOccupy, Times reserve)
    {
        _nodeStateRepositoryMock.Verify(x => x.ForceReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), forceReserve);
        _nodeStateRepositoryMock.Verify(x => x.ForceOccupyNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), forceOccupy);
        _nodeStateRepositoryMock.Verify(x => x.ReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), reserve);
    }

    public static IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(Profiles.ToArray());
        return mapper;
    }

    /// <summary>
    /// Gets the response from a request
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="request">The request object</param>
    /// <returns>The response object</returns>
    public async Task<Response<TResult>> GetResponseAsync<TResult, TRequest>(TRequest request)
        where TResult : class
        where TRequest : class
    {
        IRequestClient<TRequest> client = TestHarness.Bus!.CreateRequestClient<TRequest>();
        Response<TResult> response = await client.GetResponse<TResult>(request);
        return response;
    }

    public AgvCommandHandlerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueForAgv(It.IsAny<Guid>())).ReturnsAsync(queue);
        return this;
    }

    public AgvCommandHandlerTestFixture WithAgvRemovedFromQueueEntry(bool removed)
    {
        _queueManager.Setup(x => x.RemoveAgvFromQueueEntry(It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(removed);
        return this;
    }

    public AgvCommandHandlerTestFixture WithAgvIsInQueue(bool isInQueue)
    {
        _queueManager.Setup(x => x.IsAgvInQueue(It.IsAny<Guid>())).ReturnsAsync(isInQueue);
        return this;
    }
}