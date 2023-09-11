using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TransportOrder.Domain.Profiles;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.SemaphoreProvider;
using TransportOrder.Domain.Services.ActionGroup;
using TransportOrder.Domain.Services.Routing;
using TransportOrder.Routing;
using ActionGroup = TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

public class ActionCommandHandlerTestFixture : MassTransitConsumerTestFixture<ActionCommandHandler>
{
    public readonly Mock<IAgvSagaCommunicationService> AgvSagaCommunicationService = new();
    private readonly Mock<IRouteService> _routeService = new();
    private readonly Mock<IActionGroupRegistry> _actionGroupRegistry = new();
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly Mock<IChargingPointManager> _chargingPointManager = new();
    private readonly Mock<IParkPointService> _parkPointService = new();
    private readonly Mock<IChargingService> _chargingService = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => AgvSagaCommunicationService.Object);
        collection.AddTransient(_ => _actionGroupRegistry.Object);
        collection.AddTransient(_ => _routeService.Object);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddTransient(_ => _maintenanceService.Object);
        collection.AddTransient(_ => _chargingPointManager.Object);
        collection.AddTransient(_ => _parkPointService.Object);
        collection.AddTransient(_ => _chargingService.Object);
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
    }

    public ActionCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null)
    {
       Build();
       if (inactivityTimeout != null)
       {
           TestHarness!.TestInactivityTimeout = (TimeSpan)inactivityTimeout;
       }
       _actionGroupRegistry.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?)null);

    }

    public void AddAgv(IAgv agv)
    {
        AgvSagaCommunicationService.Setup(x =>
            x.GetAgvAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(agv);
        AgvSagaCommunicationService.Setup( x => x.SetIdleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Verifiable();
    }

    public void AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction=true)
    {
        _actionGroupRegistry.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRegistry.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(),It.IsAny<IAgv>(), It.IsAny<ActionState>())).ReturnsAsync(actionGroup);
        _actionGroupRegistry.Setup(x => x.GetSubsequentActionAsync( It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());
        _actionGroupRegistry.Setup(x => x.GetActionGroupsAsync(It.IsAny<ActionGroupState>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(),It.IsAny<bool?>())).ReturnsAsync(new List<ActionGroup>{actionGroup});
        _actionGroupRegistry.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRegistry.Setup(x => x.HasUnfinishedDropActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasUnfinishedAction);
        _actionGroupRegistry.Setup(x => x.GetRequiredActionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());
    }

    public void AddRoute(string[] nodes)
    {
        _routeService.Setup(x => x.GetRouteAsync(It.IsAny<IAgv>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new IGraphAlgorithm.Route(1f, nodes));
    }

    public void SetBusyThrowsException(bool throwException)
    {
        if (throwException)
        {
            AgvSagaCommunicationService.Setup(x =>
                x.SetBusyAsync(It.IsAny<IAgv>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Throws<Exception>();
            return;
        }
        AgvSagaCommunicationService.Setup(x =>
            x.SetBusyAsync(It.IsAny<IAgv>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Verifiable();

    }

    public void AgvIsReadyForBusy()
    {
        AgvSagaCommunicationService.Setup(x => x.IsAgvReadyForBusyAsync(It.IsAny<Guid>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(true);
    }
    public static IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(new ActionGroupProfile());
        return mapper;
    }
}