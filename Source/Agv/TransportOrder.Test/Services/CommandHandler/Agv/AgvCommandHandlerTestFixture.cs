using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.CommandHandler.Agv;

public class AgvCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvCommandHandler>
{
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<ILogger<AgvCommandHandler>> _logger = new();
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly ProductionOptions _productionOptions = new() { AutoAccept = true };
    private readonly Mock<IChargingPointManager> _chargingPointManager = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    public readonly Mock<IAgvDb> AgvDb = new();
    private readonly Mock<IParkPointService> _parkPointService = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepositoryMock = new();
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalService = new();
    private readonly Mock<IMetricCollectionService> _metricCollectorServiceMock = new();

    private static IEnumerable<Profile> Profiles => [new AgvStatusProfile(), new AgvProfile(), new CargoProfile()];

    protected override IEnumerable<Profile> GetProfiles() => Profiles;

    public AgvCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _logger.Object);
        collection.AddTransient(_ => _mapRepository.Object);
        collection.AddTransient(_ => _productionOptions);
        collection.AddTransient(_ => AgvDb.Object);
        collection.AddTransient(_ => _chargingPointManager.Object);
        collection.AddTransient(_ => _nodeStateRepository.Object);
        collection.AddTransient(_ => _parkPointService.Object);
        collection.AddTransient(_ => _nodeStateRepositoryMock.Object);
        collection.AddTransient(_ => _actionGroupRetrievalService.Object);
        collection.AddTransient(_ => _metricCollectorServiceMock.Object);
        _metricCollectorServiceMock.Setup(x => x.CollectBatteryChargeMetrics(It.IsAny<AgvStatus>())).Verifiable();
        return this;
    }

    public void VerifyBatteryMetricsCollected(Times callTimes)
    {
        _metricCollectorServiceMock.Verify(x => x.CollectBatteryChargeMetrics(It.IsAny<AgvStatus>()), callTimes);
    }

    public AgvCommandHandlerTestFixture AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        AgvDb.AddAgvsToMockRepository(agvs);
        AgvDb.Setup(r => r.Agvs.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()))
            .ReturnsAsync((TransportOrder.Domain.Models.Agv.Agv a) => agvs.Any(agv => agv.CorrelationId == a.CorrelationId))
            .Verifiable();
        AgvDb.Setup(r => r.Agvs.AddAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).Verifiable();

        return this;
    }

    [PublicAPI]
    public AgvCommandHandlerTestFixture SetupAgv(bool hasPendingActions = false, bool graphExists = true)
    {
        _actionGroupRepository.Setup(x =>
            x.AgvHasPendingActions(It.IsAny<Guid>())).ReturnsAsync(hasPendingActions);
        AgvDb.Setup(x => x.Graphs.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(graphExists);

        return this;
    }

    [PublicAPI]
    public AgvCommandHandlerTestFixture WithCurrentAgvAction(AgvAction action)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(action);
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
        AgvDb.Setup(x => x.Graphs.ExistsAsync()).ReturnsAsync(exists);
        return this;
    }

    public AgvCommandHandlerTestFixture WithoutAgv()
    {
        AgvDb.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => throw new AgvNotFoundException(id));
        return this;
    }

    public AgvCommandHandlerTestFixture AddParkingPointManager(GraphNode parkGraphNode)
    {
        _parkPointService.Setup(x => x.ReserveParkAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(parkGraphNode);
        _nodeStateRepositoryMock.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(new NodeLock(parkGraphNode.Id, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
        return this;
    }

    public AgvCommandHandlerTestFixture HasAlreadyReservedNode(bool hasNodeReserved)
    {
        _parkPointService.Setup(x => x.AgvHasReservedParkingNodeAsync(It.IsAny<Guid>())).ReturnsAsync(hasNodeReserved);
        _chargingPointManager.Setup(x => x.AgvHasReservedChargerAsync(It.IsAny<Guid>())).ReturnsAsync(hasNodeReserved);
        return this;
    }

    public AgvCommandHandlerTestFixture AddChargingPointManager(GraphNode chargeGraphNode)
    {
        _chargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), null)).ReturnsAsync(chargeGraphNode);
        _nodeStateRepositoryMock.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(new NodeLock(chargeGraphNode.Id, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
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

    public AgvCommandHandlerTestFixture WithActionGroups(List<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(),
                It.IsAny<SortBy>(), It.IsAny<bool>()))
            .ReturnsAsync(actionGroups);

        return this;
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
}