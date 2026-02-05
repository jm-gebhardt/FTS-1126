using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvActionGenerator = Gebhardt.Acc.Common.Testing.Helpers.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Services.BackgroundServices;

public class UnifiedTargetSelectorTestFixture : MassTransitTestFixture
{
    private UnifiedTargetSelector Selector { get; }

    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    private readonly SemaphoreProvider<ITargetSelectionSemaphore> _semaphore = new(new Mock<ILogger<SemaphoreProvider<ITargetSelectionSemaphore>>>().Object);
    private readonly SemaphoreProvider<IWaitingHandlerSemaphore> _waitingHandlerSemaphore = new(new Mock<ILogger<SemaphoreProvider<IWaitingHandlerSemaphore>>>().Object);
    private List<DecisionPointAction> _dp1Actions = [];
    private List<WaitingPointAction> _slwActions = [];
    private readonly Dictionary<Guid, Guid> _nodeReservations = new(); // Track which AGV has reserved which node

    public static readonly string[] StationNames = ["PS01", "PS02", "PS03"];

    public Mock<IDecisionPointActionRepository> DecisionPointRepository { get; } = new();

    public Mock<IWaitingPointActionRepository> WaitingPointRepository { get; } = new();

    public List<Station> Stations { get; private set; } = new();

    public List<ActionGroup> ActionGroups { get; } = new();

    public UnifiedTargetSelectorTestFixture()
    {
        AddServices(collection =>
        {
            collection.AddTransient(_ => _stationRepository.Object);
            collection.AddTransient(_ => _actionGroupRepository.Object);
            collection.AddTransient(_ => _nodeStateRepository.Object);
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
        DecisionPointRepository.Setup(x => x.RemoveById(It.IsAny<Guid>())).Callback((Guid id) => { _dp1Actions.RemoveAll(a => a.AgvActionCorrelationId == id); });
        DecisionPointRepository.Setup(x => x.Add(It.IsAny<DecisionPointAction>())).Callback((DecisionPointAction action) => { _dp1Actions.Add(action); });
        DecisionPointRepository.Setup(x => x.AddAsync(It.IsAny<DecisionPointAction>())).Callback((DecisionPointAction action) => _dp1Actions.Add(action));
        DecisionPointRepository.Setup(x => x.UpdateAsync(It.IsAny<DecisionPointAction>())).Verifiable();
        DecisionPointRepository.Setup(x => x.GetAll()).ReturnsAsync(() => _dp1Actions);
        DecisionPointRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _dp1Actions.Single(a => a.AgvActionCorrelationId == c));

        // Setup WaitingPointRepository
        WaitingPointRepository.Setup(x => x.RemoveById(It.IsAny<Guid>())).Callback((Guid id) => { _slwActions.RemoveAll(a => a.AgvActionCorrelationId == id); });
        WaitingPointRepository.Setup(x => x.Add(It.IsAny<WaitingPointAction>())).Callback((WaitingPointAction action) => { _slwActions.Add(action); });
        WaitingPointRepository.Setup(x => x.AddAsync(It.IsAny<WaitingPointAction>())).Callback((WaitingPointAction action) => _slwActions.Add(action));
        WaitingPointRepository.Setup(x => x.UpdateAsync(It.IsAny<WaitingPointAction>())).Verifiable();
        WaitingPointRepository.Setup(x => x.GetAll()).ReturnsAsync(() => _slwActions);
        WaitingPointRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _slwActions.Single(a => a.AgvActionCorrelationId == c));

        Selector = Provider.GetRequiredService<UnifiedTargetSelector>();
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

    private UnifiedTargetSelectorTestFixture WithStations(ICollection<Station> stations)
    {
        Stations = stations.ToList();
        _stationRepository.Setup(x => x.GetStationsById(It.IsAny<List<Guid>>())).ReturnsAsync((ICollection<Guid> ids) => stations.Where(s => ids.Contains(s.Id)).ToList());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync((Guid id) => stations.Single(s => s.Id == id));

        _nodeStateRepository.Setup(x => x.ReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync((Guid nodeId, Guid agvId, bool _) =>
            {
                if (!_nodeReservations.TryAdd(nodeId, agvId))
                {
                    // Node already reserved by another AGV
                    return false;
                }
                return true;
            });
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid nodeId) =>
            {
                var node = new Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode(nodeId, "test");
                return new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
            });
        return this;
    }

    public UnifiedTargetSelectorTestFixture WithDefaultStations() =>
        WithStations()
            .WithConsideredStations(Stations.Select(s => s.Id)
                .ToList())
            .WithWaitingTargets(Stations.Last().Id, [Stations.Skip(1).Take(1).Single().Id, Stations.First().Id]);

    public UnifiedTargetSelectorTestFixture WithStations(params (int Count, bool hasPalletJam, bool isLocked)[] stationProperties)
    {
        bool useJam = false;
        List<Station> stationList = [];
        for (int iStation = 0; iStation < StationNames.Length; iStation++)
        {
            (int count, bool hasPalletJam, bool isLocked) property = stationProperties.ElementAtOrDefault(iStation);
            string name = StationNames[iStation];
            var station = StationGenerator.GetPickDropStation(name, hasPalletJam: property.hasPalletJam, property.isLocked, property.count > 0 ? NodeLockState.Reserved : NodeLockState.Free);
            stationList.Add(station);
            if (property.hasPalletJam)
            {
                useJam = true;
            }
        }
        // Setup node reservation to track which AGV has reserved which node
        _nodeStateRepository.Setup(x => x.ReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync((Guid nodeId, Guid agvId, bool _) =>
            {
                if (!_nodeReservations.TryAdd(nodeId, agvId))
                {
                    // Node already reserved by another AGV
                    return false;
                }
                return true;
            });
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid nodeId) =>
            {
                // Return a simple NodeLock - the actual content doesn't matter for the test
                var node = new Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode(nodeId, "test");
                return new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
            });
        Selector.SetUseJamState(useJam);
        return WithStations(stationList);
    }

    public UnifiedTargetSelectorTestFixture WithDefaultConfiguration()
    {
        return WithConsideredStations(Stations.Select(s => s.Id)
                .ToList())
            .WithWaitingTargets(Stations.Last().Id, [Stations.Skip(1).Take(1).Single().Id, Stations.First().Id]);
    }

    public UnifiedTargetSelectorTestFixture WithConsideredStations(ICollection<Guid> stations)
    {
        Selector.SetStationCount(stations);
        return this;
    }

    public UnifiedTargetSelectorTestFixture WithWaitingTargets(Guid primaryTarget, ICollection<Guid> secondaryTargets)
    {
        Selector.ConfigureTargets(primaryTarget, secondaryTargets);
        return this;
    }

    public UnifiedTargetSelectorTestFixture AddPickActionGroup(Guid? assignedAgv = null)
    {
        var pickActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutPickTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        pickActionGroupMessage.AssignedAgv = assignedAgv ?? Guid.NewGuid();
        var actionGroup = Helper.AgvActionGenerator.ConvertFromMessage(pickActionGroupMessage);

        return AddActionGroup(actionGroup);
    }

    public UnifiedTargetSelectorTestFixture AddDropActionGroup(Guid? assignedAgv = null)
    {
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = assignedAgv ?? Guid.NewGuid();
        var actionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);
        return AddActionGroup(actionGroup);
    }

    public UnifiedTargetSelectorTestFixture AddActionGroup(ActionGroup actionGroup)
    {
        ActionGroups.Add(actionGroup);
        _actionGroupRepository.Setup(x => x.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => ActionGroups.Single(a => a.Actions.Any(ac => ac.CorrelationId == id)));
        return this;
    }

    public async Task ExecuteAsync(float waitingFactor = 0.9f)
    {
        var cancellationToken = new CancellationTokenSource();
        _ = Selector.StartAsync(cancellationToken.Token);
        // We have to wait a bit to make sure that we are past the evaluation of cancellation token (happens only once at the beginning of the background task).
        // But we may not wait longer than the WaitingTime, as this might have lead to a second execution of the background loop already.
        // This is no ideal implementation, but a quick one which serves the current implementation to make sure the logic is executed once.
        await Task.Delay(Selector.WaitingTime * waitingFactor, cancellationToken.Token);
        await cancellationToken.CancelAsync();
        await Selector.StopAsync(CancellationToken.None);
    }

    public ITargetSelector AsTargetSelector() => Selector;

    public IWaitingHandler AsWaitingHandler() => Selector;
}