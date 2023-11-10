using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.ActionGroup;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.ActionGroup;

public class ActionGroupControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, ActionGroupController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private readonly Mock<IActionGroupRetrievalService> _retrievalServiceMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepositoryMock = new();
    private readonly Mock<IRouteService> _routeService = new();
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly Mock<IChargingPointManager> _chargingPointManager = new();
    private readonly Mock<IParkPointService> _parkPointService = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();
    private readonly Mock<ISettingsProvider<AgvSettings>> _settingsProvider = new();

    private readonly List<ActionGroupReadModel> _actionGroupReadModels = new();
    private readonly List<Domain.Models.ActionGroup.ActionGroup> _actionGroups = new();

    protected override ActionGroupController GetController()
    {
        IRequestClient<GetActionGroup> getActionGroupRequestClient = GetRequestClient<GetActionGroup>();
        IRequestClient<TriggerActionGroupAbortion> abortionRequestClient = GetRequestClient<TriggerActionGroupAbortion>();
        IRequestClient<CancelActionGroup> cancelActionGroupRequestClient = GetRequestClient<CancelActionGroup>();
        var controller = new ActionGroupController(GetMapper(), 
            getActionGroupRequestClient, cancelActionGroupRequestClient, abortionRequestClient)
        {
            ControllerContext = new ControllerContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ControllerActionDescriptor()))
        };
        return controller;
    }

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> Setup(IBus? bus = null)
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _retrievalServiceMock.Object);
            sc.AddTransient(_ => _actionGroupRepositoryMock.Object);
            sc.AddTransient(_ => _routeService.Object);
            sc.AddTransient(_ => _chooseAgvActionGroupService.Object);
            sc.AddTransient(_ => _maintenanceService.Object);
            sc.AddTransient(_ => _chargingPointManager.Object);
            sc.AddTransient(_ => _parkPointService.Object);
            sc.AddTransient(_ => GetMapper());
            sc.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
            sc.AddTransient(_ => _agvRepository.Object);
            sc.AddTransient(_ => _actionGroupValidationService.Object);
            sc.AddTransient(_ => _settingsProvider.Object);
            if (bus != null)
            {
               sc.AddTransient(_ => bus);
            }
            else
            {
                var busMock = new Mock<IBus>();
               sc.AddTransient(_ => busMock.Object);
            }
        });

        _retrievalServiceMock.Setup(x => x.GetActionGroupWithNodesAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actionGroupReadModels.FirstOrDefault(a => a.CorrelationId == c));
        _actionGroupRepositoryMock.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
        {
            var ag = _actionGroups.FirstOrDefault(a => a.CorrelationId == c);
            return ag ?? throw new ActionGroupNotFoundException(c);
        });
        _actionGroupRepositoryMock.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actionGroups.FirstOrDefault(a => a.CorrelationId == c));
        _actionGroupRepositoryMock.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<Domain.Models.ActionGroup.AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<ActionState>()))!.ReturnsAsync(
            (Domain.Models.ActionGroup.AgvAction action, Domain.Models.Agv.Agv _, ActionState _) => _actionGroups.FirstOrDefault(a => a.CorrelationId == action.ParentId));
        _retrievalServiceMock.Setup(x => x.EnrichActionGroupWithNodesAsync(It.IsAny<Domain.Models.ActionGroup.ActionGroup>())).ReturnsAsync((Domain.Models.ActionGroup.ActionGroup ag) => ag.ConvertFromActionGroupWithFakeNodeType()!);
        return this;
    }

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> AddActionGroup(ActionGroupReadModel actionGroup)
    {
        _actionGroupReadModels.Add(actionGroup);
        return this;
    }

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> AddActionGroup(Common.Messages.ActionGroups.ActionGroup actionGroup)
    {
        var mapper = GetMapper();
        _actionGroups.Add(mapper.Map<Domain.Models.ActionGroup.ActionGroup>(actionGroup));
        return this;
    }

    public new ActionGroupControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();

        return this;
    }
}