using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.BackgroundServices;

public class TargetSelectorTestFixture: MassTransitTestFixture
{
    public TargetSelector Selector { get; private set; }
   
    private readonly IStationRepository _repository;
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();
    private readonly SemaphoreProvider<ITargetSelectionSemaphore> _semaphore = new();
    private readonly List<ActionGroup> _actionGroups = new();
    private readonly Mock<IDecisionPointActionRepository> _decisionPointRepository = new();
    private readonly List<DecisionPointAction> _actions = [];

    public TargetSelectorTestFixture()
    {
        AddServices(collection =>
        {
            collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
            collection.AddSingleton<IStationRepository, StationRepository>();
            collection.AddTransient(_ => _transportOrderDb.Object);
            collection.AddSingleton<ITargetSelector, TargetSelector>();
            collection.AddSingleton<IHostedService>(sp => (IHostedService)sp.GetRequiredService<ITargetSelector>());
            collection.AddSingleton<ISemaphoreProvider<ITargetSelectionSemaphore>>(_ => _semaphore);
            collection.AddTransient(_ => _decisionPointRepository.Object);
        });

        Build();
        _decisionPointRepository.Setup(x => x.RemoveById(It.IsAny<Guid>())).Verifiable();
        _decisionPointRepository.Setup(x => x.Add(It.IsAny<DecisionPointAction>())).Callback((DecisionPointAction action) => _actions.Add(action));
        _decisionPointRepository.Setup(x => x.UpdateAsync(It.IsAny<DecisionPointAction>())).Verifiable();
        _decisionPointRepository.Setup(x => x.GetAll()).ReturnsAsync(_actions);
        _decisionPointRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actions.Single(a => a.CorrelationId == c));
        _repository = Provider.GetRequiredService<IStationRepository>();
        Selector = (Provider.GetServices<IHostedService>().First(x => x is ITargetSelector) as TargetSelector)!;
        _transportOrderDb.Setup(x => x.ActionGroups.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
        {
            return _actionGroups.Single(a => a.Actions.Any(ac => ac.CorrelationId== c));
        });
    }

    public TargetSelectorTestFixture WithOccupancies(ICollection<StationOccupancy> occupancies)
    {
        foreach (var occupancy in occupancies)
        {
            foreach (int unused in Enumerable.Range(0, occupancy.Count))
            {
                _repository.OccupyStation(occupancy.Station, Guid.NewGuid());
            }
        }
        return this;
    }

    public TargetSelectorTestFixture WithStations(ICollection<Station> stations)
    {
        _transportOrderDb.Setup(x => x.Stations.GetStationsByNames(It.IsAny<string[]>())).ReturnsAsync(stations);
        return this;
    }

    public TargetSelectorTestFixture AddActionGroup(ActionGroup actionGroup)
    {
        _actionGroups.Add(actionGroup);
        return this;
    }
}