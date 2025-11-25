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
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.BackgroundServices;

public class TargetSelectorTestFixture : MassTransitTestFixture
{
    public TargetSelector Selector { get; }

    private readonly StationRepository _stationRepository;
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();
    private readonly Mock<IJamStateRepository> _jamStateRepository = new();
    private readonly SemaphoreProvider<ITargetSelectionSemaphore> _semaphore = new(new Mock<ILogger<SemaphoreProvider<ITargetSelectionSemaphore>>>().Object);
    private List<DecisionPointAction> _actions = [];

    public Mock<IDecisionPointActionRepository> DecisionPointRepository { get; } = new();

    public List<Station> Stations { get; private set; } = new();

    public List<ActionGroup> ActionGroups { get; } = new();

    public TargetSelectorTestFixture()
    {
        AddServices(collection =>
        {
            collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
            collection.AddSingleton<IStationRepository, StationRepository>();
            collection.AddTransient(_ => _transportOrderDb.Object);
            collection.AddTransient(_ => _jamStateRepository.Object);
            collection.AddSingleton<ITargetSelector, TargetSelector>();
            collection.AddSingleton<ISemaphoreProvider<ITargetSelectionSemaphore>>(_ => _semaphore);
            collection.AddTransient(_ => DecisionPointRepository.Object);
        });

        Build();
        DecisionPointRepository.Setup(x => x.RemoveById(It.IsAny<Guid>())).Callback((Guid id) => { _actions.RemoveAll(a => a.CorrelationId == id); });
        DecisionPointRepository.Setup(x => x.Add(It.IsAny<DecisionPointAction>())).Callback((DecisionPointAction action) => { _actions.Add(action); });
        DecisionPointRepository.Setup(x => x.AddAsync(It.IsAny<DecisionPointAction>())).Callback((DecisionPointAction action) => _actions.Add(action));
        DecisionPointRepository.Setup(x => x.UpdateAsync(It.IsAny<DecisionPointAction>())).Verifiable();
        DecisionPointRepository.Setup(x => x.GetAll()).ReturnsAsync(() => _actions);
        DecisionPointRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actions.Single(a => a.CorrelationId == c));
        _stationRepository = (Provider.GetRequiredService<IStationRepository>() as StationRepository)!;
        Selector = (Provider.GetRequiredService<ITargetSelector>() as TargetSelector)!;
        _transportOrderDb.Setup(x => x.ActionGroups.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => { return ActionGroups.Single(a => a.Actions.Any(ac => ac.CorrelationId == c)); });
    }

    public TargetSelectorTestFixture WithDecisionPointAction(params DecisionPointAction[] actions)
    {
        _actions = actions.ToList();
        return this;
    }

    private TargetSelectorTestFixture WithOccupancies(ICollection<StationOccupancy> occupancies)
    {
        _stationRepository.WithOccupancies(occupancies);
        return this;
    }

    private TargetSelectorTestFixture WithStations(ICollection<Station> stations)
    {
        Stations = stations.ToList();
        _transportOrderDb.Setup(x => x.Stations.GetStationsByIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((ICollection<Guid> ids) => stations.Where(s => ids.Contains(s.Id)).ToList());
        return this;
    }

    public TargetSelectorTestFixture WithDefaultOccupancies() => WithOccupancies();

    public TargetSelectorTestFixture WithOccupancies(params (int Count, Guid? AgvId)[] occupiedCountAndAgv)
    {
        string[] stationNames = ["1", "2", "3"];
        Dictionary<string, Guid> stations = stationNames.ToDictionary(n => n, n => n.ToGuid());

        List<StationOccupancy> occupancyList = stations
            .Select((station, index) =>
            {
                (int Count, Guid? AgvId) value = occupiedCountAndAgv.ElementAtOrDefault(index);
                return new StationOccupancy(station.Value, value.Count, value.AgvId);
            })
            .ToList();

        List<Station> stationList = stationNames
            .Select(id => new Station(id, AgvGraph.NodeType.PickDrop, []))
            .ToList();

        return WithOccupancies(occupancyList)
            .WithStations(stationList);
    }

    public TargetSelectorTestFixture WithConsideredStations(ICollection<Guid> stations)
    {
        Selector.SetStationCount(stations);
        return this;
    }

    public TargetSelectorTestFixture AddPickActionGroup()
    {
        var pickActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutPickTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        pickActionGroupMessage.AssignedAgv = Guid.NewGuid();
        var actionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(pickActionGroupMessage);

        return AddActionGroup(actionGroup);
    }

    public TargetSelectorTestFixture AddDropActionGroup(Guid? assignedAgv = null)
    {
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = assignedAgv ?? Guid.NewGuid();
        var actionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);

        return AddActionGroup(actionGroup);
    }

    public TargetSelectorTestFixture AddActionGroup(ActionGroup actionGroup)
    {
        ActionGroups.Add(actionGroup);
        return this;
    }

    public Task FreeStation(Guid stationId)
    {
        return _stationRepository.FreeStation(stationId);
    }

    public async Task SelectTargetAsync()
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

    public TargetSelectorTestFixture WithStationHasPalletJam(Guid? stationId)
    {
        Selector.SetUseJamState(stationId != null);
        _jamStateRepository.Setup(x => x.GetByStationAsync(It.IsAny<Guid>())).ReturnsAsync((Guid s) => new JamState(s, s == stationId ? 1 : 0, DateTime.UtcNow));
        return this;
    }
}