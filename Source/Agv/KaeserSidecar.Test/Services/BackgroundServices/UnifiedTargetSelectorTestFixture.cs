using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Domain;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using Station = Gebhardt.Acc.TransportOrder.Domain.Models.Station.Station;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.BackgroundServices;

public class UnifiedTargetSelectorTestFixture : MassTransitTestFixture
{
    private UnifiedTargetSelector Selector { get; }

    private readonly StationRepository _stationRepository;
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();
    private readonly Mock<IJamStateRepository> _jamStateRepository = new();
    private readonly Mock<IPrismatCommunicationShortLoopWeTargetStationRepository> _shortLoopTargetStationRepository = new();
    private readonly SemaphoreProvider<ITargetSelectionSemaphore> _semaphore = new(new Mock<ILogger<SemaphoreProvider<ITargetSelectionSemaphore>>>().Object);
    private readonly SemaphoreProvider<IWaitingHandlerSemaphore> _waitingHandlerSemaphore = new(new Mock<ILogger<SemaphoreProvider<IWaitingHandlerSemaphore>>>().Object);
    private List<DecisionPointAction> _dp1Actions = [];
    private List<WaitingPointAction> _slwActions = [];

    public static readonly string[] StationNames = ["1", "2", "3"];

    public Mock<IDecisionPointActionRepository> DecisionPointRepository { get; } = new();

    public Mock<IWaitingPointActionRepository> WaitingPointRepository { get; } = new();

    public List<Station> Stations { get; private set; } = new();

    public List<ActionGroup> ActionGroups { get; } = new();

    private const string ShortLoopTarget = "PS03";

    public static Guid ShortLoopTargetId = ShortLoopTarget.ToGuid();

    public UnifiedTargetSelectorTestFixture()
    {
        AddServices(collection =>
        {
            collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
            collection.AddSingleton<IStationRepository, StationRepository>();
            collection.AddTransient(_ => _transportOrderDb.Object);
            collection.AddTransient(_ => _jamStateRepository.Object);
            collection.AddTransient(_ => _shortLoopTargetStationRepository.Object);
            collection.AddSingleton<PriorityAwareTargetSelector>();
            collection.AddSingleton<PriorityAwareWaitingHandler>();
            collection.AddSingleton<UnifiedTargetSelector>();
            collection.AddSingleton<ITargetSelector>(sp => sp.GetRequiredService<UnifiedTargetSelector>());
            collection.AddSingleton<IWaitingHandler>(sp => sp.GetRequiredService<UnifiedTargetSelector>());
            collection.AddSingleton<ISemaphoreProvider<ITargetSelectionSemaphore>>(_ => _semaphore);
            collection.AddSingleton<ISemaphoreProvider<IWaitingHandlerSemaphore>>(_ => _waitingHandlerSemaphore);
            collection.AddTransient(_ => DecisionPointRepository.Object);
            collection.AddTransient(_ => WaitingPointRepository.Object);
        });

        Build();

        // Setup DecisionPointRepository
        DecisionPointRepository.Setup(x => x.RemoveById(It.IsAny<Guid>())).Callback((Guid id) => { _dp1Actions.RemoveAll(a => a.CorrelationId == id); });
        DecisionPointRepository.Setup(x => x.Add(It.IsAny<DecisionPointAction>())).Callback((DecisionPointAction action) => { _dp1Actions.Add(action); });
        DecisionPointRepository.Setup(x => x.AddAsync(It.IsAny<DecisionPointAction>())).Callback((DecisionPointAction action) => _dp1Actions.Add(action));
        DecisionPointRepository.Setup(x => x.UpdateAsync(It.IsAny<DecisionPointAction>())).Verifiable();
        DecisionPointRepository.Setup(x => x.GetAll()).ReturnsAsync(() => _dp1Actions);
        DecisionPointRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _dp1Actions.Single(a => a.CorrelationId == c));

        // Setup WaitingPointRepository
        WaitingPointRepository.Setup(x => x.RemoveById(It.IsAny<Guid>())).Callback((Guid id) => { _slwActions.RemoveAll(a => a.CorrelationId == id); });
        WaitingPointRepository.Setup(x => x.Add(It.IsAny<WaitingPointAction>())).Callback((WaitingPointAction action) => { _slwActions.Add(action); });
        WaitingPointRepository.Setup(x => x.AddAsync(It.IsAny<WaitingPointAction>())).Callback((WaitingPointAction action) => _slwActions.Add(action));
        WaitingPointRepository.Setup(x => x.UpdateAsync(It.IsAny<WaitingPointAction>())).Verifiable();
        WaitingPointRepository.Setup(x => x.GetAll()).ReturnsAsync(() => _slwActions);
        WaitingPointRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _slwActions.Single(a => a.CorrelationId == c));

        _stationRepository = (Provider.GetRequiredService<IStationRepository>() as StationRepository)!;
        Selector = Provider.GetRequiredService<UnifiedTargetSelector>();

        _transportOrderDb.Setup(x => x.ActionGroups.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => { return ActionGroups.Single(a => a.Actions.Any(ac => ac.CorrelationId == c)); });

        // Default short loop target setup
        _shortLoopTargetStationRepository.Setup(x => x.GetTargetStation()).ReturnsAsync(new ShortLoopWeTargetStation(Guid.NewGuid(), ShortLoopTarget, ShortLoopTarget));
    }

    public UnifiedTargetSelectorTestFixture WithDecisionPointAction(params DecisionPointAction[] actions)
    {
        _dp1Actions = actions.ToList();
        return this;
    }

    public UnifiedTargetSelectorTestFixture WithWaitingPointAction(params WaitingPointAction[] actions)
    {
        _slwActions = actions.ToList();
        return this;
    }

    private UnifiedTargetSelectorTestFixture WithOccupancies(ICollection<StationOccupancy> occupancies)
    {
        _stationRepository.WithOccupancies(occupancies);
        return this;
    }

    private UnifiedTargetSelectorTestFixture WithStations(ICollection<Station> stations)
    {
        Stations = stations.ToList();
        _transportOrderDb.Setup(x => x.Stations.GetStationsByIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((ICollection<Guid> ids) => stations.Where(s => ids.Contains(s.Id)).ToList());
        return this;
    }

    public UnifiedTargetSelectorTestFixture WithDefaultOccupancies() => WithOccupancies();

    public UnifiedTargetSelectorTestFixture WithOccupancies(params (int Count, Guid? AgvId)[] occupiedCountAndAgv)
    {
        Dictionary<string, Guid> stations = StationNames.ToDictionary(n => n, n => n.ToGuid());

        List<StationOccupancy> occupancyList = stations
            .Select((station, index) =>
            {
                (int Count, Guid? AgvId) value = occupiedCountAndAgv.ElementAtOrDefault(index);
                return new StationOccupancy(station.Value, value.Count, value.AgvId);
            })
            .ToList();

        List<Station> stationList = StationNames
            .Select(id => new Station(id, AgvGraph.NodeType.PickDrop, []))
            .ToList();
        return WithOccupancies(occupancyList).
            WithStations(stationList);
    }

    public UnifiedTargetSelectorTestFixture WithConsideredStations(ICollection<Guid> stations)
    {
        Selector.SetStationCount(stations);
        return this;
    }

    public UnifiedTargetSelectorTestFixture AddPickActionGroup(Guid? assignedAgv = null)
    {
        var pickActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutPickTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        pickActionGroupMessage.AssignedAgv = assignedAgv ?? Guid.NewGuid();
        var actionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(pickActionGroupMessage);

        return AddActionGroup(actionGroup);
    }

    public UnifiedTargetSelectorTestFixture AddDropActionGroup(Guid? assignedAgv = null)
    {
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = assignedAgv ?? Guid.NewGuid();
        var actionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);

        return AddActionGroup(actionGroup);
    }

    public UnifiedTargetSelectorTestFixture AddShortCircuitActionGroup(Guid? assignedAgv = null)
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, assignedAgv ?? Guid.NewGuid());
        return AddActionGroup(TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(actionGroup));
    }

    public UnifiedTargetSelectorTestFixture AddActionGroup(ActionGroup actionGroup)
    {
        ActionGroups.Add(actionGroup);
        return this;
    }

    public async Task ExecuteAsync()
    {
        var cancellationToken = new CancellationTokenSource();
        _ = Selector.StartAsync(cancellationToken.Token);
        // We have to wait a bit to make sure that we are past the evaluation of cancellation token (happens only once at the beginning of the background task).
        // But we may not wait longer than the WaitingTime, as this might have lead to a second execution of the background loop already.
        // This is no ideal implementation, but a quick one which serves the current implementation to make sure the logic is executed once.
        await Task.Delay(Selector.WaitingTime * 0.9, cancellationToken.Token);
        await cancellationToken.CancelAsync();
        await Selector.StopAsync(CancellationToken.None);
    }

    public async Task VerifyStationOccupancyAsync(Guid stationId, int count, Guid? agvCorrelationId = null)
    {
        ICollection<StationOccupancy> stationOccupancies = await _stationRepository.GetStationOccupancy();
        var stationOccupancy = stationOccupancies.SingleOrDefault(s => s.StationId == stationId);
        stationOccupancy.ShouldNotBeNull();
        stationOccupancy.Count.ShouldBe(count);
        if (stationOccupancy.Count == 0)
        {
            stationOccupancy.AgvCorrelationId.ShouldBeNull();
        }
        else
        {
            if (agvCorrelationId != null)
            {
                stationOccupancy.AgvCorrelationId.ShouldBe(agvCorrelationId);
            }
        }
    }

    public UnifiedTargetSelectorTestFixture WithStationHasPalletJam(Guid? stationId)
    {
        Selector.SetUseJamState(stationId != null);
        _jamStateRepository.Setup(x => x.GetByStationAsync(It.IsAny<Guid>())).ReturnsAsync((Guid s) => new JamState(s, s == stationId ? 1 : 0, DateTime.UtcNow));
        return this;
    }

    public UnifiedTargetSelectorTestFixture WithShortLoopTarget(string targetStation)
    {
        _shortLoopTargetStationRepository.Setup(x => x.GetTargetStation()).ReturnsAsync(new ShortLoopWeTargetStation(Guid.NewGuid(), targetStation, targetStation));
        return this;
    }

    public ITargetSelector AsTargetSelector() => Selector;

    public IWaitingHandler AsWaitingHandler() => Selector;
}