using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.Station;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

public class DoppelspielCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null) : ActionGroupTestFixtureBase<DoppelspielCommandHandler>(inactivityTimeout)
{
    private readonly Mock<IStationAgvLimitGuard> _stationLimitGuard = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly SemaphoreProvider<IDoppelspielSemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IDoppelspielSemaphore>>>().Object);

    protected override ActionGroupTestFixtureBase<DoppelspielCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _stationLimitGuard.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddSingleton<ISemaphoreProvider<IDoppelspielSemaphore>>(_ => _semaphoreProvider);
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithStationGroupLimit(bool isWithinLimit)
    {
        _stationLimitGuard.Setup(x => x.IsWithinLimitAsync(It.IsAny<ActionGroup>())).ReturnsAsync(isWithinLimit);
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithStation(TransportOrder.Domain.Models.Station.Station station)
    {
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(station);
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithNextPickForStation(ActionGroup? actionGroup)
    {
        ActionGroupRepository.Setup(x => x.GetNextPickForStation(It.IsAny<GraphNode>())).ReturnsAsync(actionGroup);
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithNextDelayedDropForStations(ActionGroup? actionGroup)
    {
        ActionGroupRepository.Setup(x => x.GetNextDelayedDropForStations(It.IsAny<ICollection<Guid>>())).ReturnsAsync(actionGroup);
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithDropAction(ActionGroup? actionGroup)
    {
        ActionGroupRepository.Setup(x => x.GetNextDropActionGroupEligibleForDoppelspiel(It.IsAny<ICollection<string>>())).ReturnsAsync(actionGroup);
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithUnmatchedPick(ActionGroup? actionGroup)
    {
        ActionGroupRepository.Setup(x => x.GetNextUnmatchedDoppelspielPick(It.IsAny<GraphNode>(), It.IsAny<Guid?>())).ReturnsAsync(actionGroup);
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithAgvMustCharge(bool mustCharge = true)
    {
        SettingsReadModel = new AgvSettingsReadModel() { BatteryLevelCritical = mustCharge ? 1000 : -1000 };
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithCreatedAndStartedActionGroups(ICollection<ActionGroup> actionGroups)
    {
        ActionGroupRepository.Setup(x => x.GetCreatedAndStartedActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups.ToList());
        return this;
    }

    public DoppelspielCommandHandlerTestFixture WithCurrentAgvAction(TransportOrder.Domain.Models.ActionGroup.AgvAction? agvAction)
    {
        ActionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(agvAction);
        return this;
    }
}