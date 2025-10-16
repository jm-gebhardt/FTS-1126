using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

public class AgvEventHandlerTestFixture : MassTransitConsumerTestFixture<AgvEventHandler>
{
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly SemaphoreProvider<IAgvsSemaphore> _agvSemaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IAgvsSemaphore>>>().Object);
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    private readonly Mock<IChargingPointManager> _chargingPointManager = new();
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IQueueManager> _queueManager = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    public const int BatteryLevelCritical = 10;
    public const int BatteryLevelLow = 40;
    public const int MinChargeUp = 80;
    public const int MaxChargeUp = 100;

    protected override IEnumerable<Profile> GetProfiles() => [new AgvStatusProfile()];

    protected override MassTransitConsumerTestFixture<AgvEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _maintenanceService.Object);
        collection.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _agvSemaphoreProvider);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _nodeStateRepository.Object);
        collection.AddTransient(_ => _queueRepository.Object);
        collection.AddTransient(_ => _chargingPointManager.Object);
        collection.AddTransient(_ => _queueManager.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient(_ => _logEntryRepository.Object);
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        return this;
    }

    public AgvEventHandlerTestFixture()
    {
        _agvDb.SetupNodeFallbacks();
        Build();
    }

    public AgvEventHandlerTestFixture WithAgvGraph(AgvGraph agvGraph)
    {
        _agvDb.AddAgvGraph(agvGraph);
        return this;
    }

    public AgvEventHandlerTestFixture WithMinBatteryAgvs(ICollection<Agv> agvs)
    {
        _agvDb.Setup(x => x.Agvs.GetChargingAgvsWithAtLeastMinChargeUpLevel()).ReturnsAsync(agvs);
        return this;
    }

    public AgvEventHandlerTestFixture WithAgv(Agv agv, bool isOnQueueHead = false)
    {
        _agvDb.AddAgvs(new List<Agv> { agv });
        _queueManager.Setup(x => x.GetAgvsAtQueueHeads()).ReturnsAsync(isOnQueueHead ? [agv.CorrelationId] : []);
        return this;
    }

    public AgvEventHandlerTestFixture WithInfiniteCharging(bool infiniteCharging)
    {
        var agvSettings = new AgvSettingsReadModel(15000, 20000, BatteryLevelCritical, BatteryLevelLow, MinChargeUp, MaxChargeUp, infiniteCharging);
        _agvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(agvSettings);
        return this;
    }

    public void AddChargeAction(ActionGroup actionGroup = null!, bool existsPriorToTest = true)
    {
        if (existsPriorToTest)
        {
            _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(new List<ActionGroup> { actionGroup });
        }
        else
        {
            _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(new List<ActionGroup>());
        }
    }

    public void AgvHasUnfinishedDropAction(Guid correlationId)
    {
        _actionGroupRepository.Setup(r => r.HasUnfinishedDropActionsAsync(It.Is<Guid>(c => c == correlationId))).ReturnsAsync(true);
        _actionGroupRepository.Setup(r => r.HasUnfinishedActionGroupAsync(It.Is<Guid>(c => c == correlationId))).ReturnsAsync(true);
    }

    public AgvEventHandlerTestFixture WithActionGroups(List<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<ActionGroupState>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 0, It.IsAny<Guid>(), It.IsAny<bool?>())).ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(
            It.IsAny<IList<ActionGroupState>?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<Guid?>(),
            It.IsAny<bool?>(),
            It.IsAny<SortBy>(),
            It.IsAny<bool>()
        )).ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetCreatedAndStartedActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetPendingActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups);

        return this;
    }

    public AgvEventHandlerTestFixture WithReservedCharger(string? charger)
    {
        _chargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<Agv>(), It.IsAny<Guid?>()))
            .ReturnsAsync((Agv _, Guid? _) =>
            {
                if (charger == null)
                {
                    throw new NoChargeStationAvailableException();
                }
                return charger.ToGraphNode();
            });
        return this;
    }

    public AgvEventHandlerTestFixture WithQueue(Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueForAgv(It.IsAny<Guid>())).ReturnsAsync(queue);
        return this;
    }

    public AgvEventHandlerTestFixture WithActiveErrors(IList<AgvError> errors)
    {
        _agvDb.AddActiveErrors(errors);
        return this;
    }

    public AgvEventHandlerTestFixture WithNode(GraphNode node)
    {
        _agvDb.AddNode(node);
        return this;
    }
}