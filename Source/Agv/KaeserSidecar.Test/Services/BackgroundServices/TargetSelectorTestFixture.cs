using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Domain;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
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
        DecisionPointRepository.Setup(x => x.RemoveById(It.IsAny<Guid>())).Verifiable();
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
        _transportOrderDb.Setup(x => x.Stations.GetStationsByNames(It.IsAny<string[]>())).ReturnsAsync(stations);
        return this;
    }

    public TargetSelectorTestFixture WithDefaultOccupancies() => WithOccupancies();

    public TargetSelectorTestFixture WithOccupancies(params (int Count, Guid? AgvId)[] occupiedCountAndAgv)
    {
        string[] stationIds = new[] { "1", "2", "3" };

        List<StationOccupancy> occupancyList = stationIds
            .Select((id, index) =>
            {
                (int Count, Guid? AgvId) value = occupiedCountAndAgv.ElementAtOrDefault(index);
                return new StationOccupancy(id, value.Count, value.AgvId);
            })
            .ToList();

        List<Station> stationList = stationIds
            .Select(id => new Station(id, AgvGraph.NodeType.PickDrop, []))
            .ToList();

        return WithOccupancies(occupancyList)
            .WithStations(stationList);
    }

    public TargetSelectorTestFixture WithStationCount(int stationCount)
    {
        Selector.SetStationCount(stationCount);
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

    public Task FreeStation(string stationName, Guid agvCorrelationId)
    {
        return _stationRepository.FreeStation(stationName, agvCorrelationId);
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

    public async Task VerifyStationOccupancyAsync(string station, int count, Guid? agvCorrelationId = null)
    {
        ICollection<StationOccupancy> stationOccupancies = await _stationRepository.GetStationOccupancy();
        var stationOccupancy = stationOccupancies.SingleOrDefault(s => s.Station == station);
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

    public TargetSelectorTestFixture WithStationHasPalletJam(string? station)
    {
        Selector.SetUseJamState(station != null);
        _jamStateRepository.Setup(x => x.GetByStationAsync(It.IsAny<string>())).ReturnsAsync((string s) => new JamState(s, string.Equals(s, station) ? 1 : 0, DateTime.UtcNow));
        return this;
    }
}