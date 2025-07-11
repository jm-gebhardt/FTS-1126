using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.RequestHandlers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.ActionGroup;

public class ActionGroupControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, ActionGroupController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private readonly Mock<IActionGroupRetrievalService> _retrievalServiceMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepositoryMock = new();
    private readonly Mock<IStationRepository> _stationRepository= new();
    private readonly Mock<IRouteService> _routeService = new();
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IGraphRepository> _graphRepositoryMock = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();
    private readonly List<ActionGroupReadModel> _actionGroupReadModels = new();
    private readonly List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> _actionGroups = new();
    private readonly Mock<IActionGroupDurationEstimationService> _actionGroupDurationEstimationService = new();
    private readonly Mock<IQueueManager> _queueManager = new();

    public ActionGroupControllerTestFixture()
    {
        AddConsumers<SuccessRequestHandlerMock<AbortActionGroup>>();
        AddConsumers<SuccessRequestHandlerMock<ResetActionGroup>>();
        
        AddServices(sc =>
        {
            sc.AddTransient(_ => _retrievalServiceMock.Object);
            sc.AddTransient(_ => _actionGroupRepositoryMock.Object);
            sc.AddTransient(_ => _stationRepository.Object);
            sc.AddTransient(_ => _actionGroupDurationEstimationService.Object);
            sc.AddTransient(_ => _routeService.Object);
            sc.AddTransient(_ => _chooseAgvActionGroupService.Object);
            sc.AddTransient(_ => _maintenanceService.Object);
            sc.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
            sc.AddTransient(_ => _agvDb.Object);
            sc.AddTransient(_ => _graphRepositoryMock.Object);
            sc.AddTransient(_ => _actionGroupValidationService.Object);
            sc.AddTransient<IMetricCollectionService, PrometheusMetricCollectionService>();
            sc.AddTransient<ActionCommandHelper>();
            sc.AddTransient(_ => _queueManager.Object);
        });
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<string>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetRequiredStationBaseInformation(It.IsAny<string>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _retrievalServiceMock.Setup(x => x.GetActionGroupWithNodesAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actionGroupReadModels.FirstOrDefault(a => a.CorrelationId == c));
        _actionGroupRepositoryMock.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
        {
            var ag = _actionGroups.FirstOrDefault(a => a.CorrelationId == c);
            return ag ?? throw new ActionGroupNotFoundException(c);
        });
        _actionGroupRepositoryMock.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actionGroups.FirstOrDefault(a => a.CorrelationId == c));
        _actionGroupRepositoryMock.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<ActionState>(), It.IsAny<Reason?>()))!.ReturnsAsync(
            (AgvAction action, TransportOrder.Domain.Models.Agv.Agv _, ActionState _, Reason? _) => _actionGroups.FirstOrDefault(a => a.CorrelationId == action.ParentId));
        _retrievalServiceMock.Setup(x => x.EnrichActionGroupWithNodesAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>())).ReturnsAsync((TransportOrder.Domain.Models.ActionGroup.ActionGroup ag) => ag.ConvertFromActionGroupWithFakeNodeType()!);
        _graphRepositoryMock.Setup(x => x.NodeExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
    }

    protected override ActionGroupController GetController()
    {
        IRequestClient<GetActionGroup> getActionGroupRequestClient = GetRequestClient<GetActionGroup>();
        IRequestClient<TriggerActionGroupAbortion> abortionRequestClient = GetRequestClient<TriggerActionGroupAbortion>();
        IRequestClient<CancelActionGroup> cancelActionGroupRequestClient = GetRequestClient<CancelActionGroup>();
        IRequestClient<TriggerActionGroupReset> triggerResetRequestClient = GetRequestClient<TriggerActionGroupReset>();
        IRequestClient<CreatePickDropActionGroup> createActionGroupRequestClient = GetRequestClient<CreatePickDropActionGroup>();
        IRequestClient<TryReassignActionGroup> reassignActionGroupClient = GetRequestClient<TryReassignActionGroup>();
        IRequestClient<TriggerActionCompletion> actionCompletionClient = GetRequestClient<TriggerActionCompletion>();
        IRequestClient<TriggerActionGroupQueueing> actionGroupQueueingClient = GetRequestClient<TriggerActionGroupQueueing>();
        IRequestClient<TriggerSetTarget> setTargetClient = GetRequestClient<TriggerSetTarget>();

        var controller = new ActionGroupController(Mapper,
            getActionGroupRequestClient,
            cancelActionGroupRequestClient,
            abortionRequestClient,
            triggerResetRequestClient,
            createActionGroupRequestClient,
            reassignActionGroupClient,
            actionCompletionClient,
            actionGroupQueueingClient,
            setTargetClient)
        {
            ControllerContext = new ControllerContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ControllerActionDescriptor()))
        };
        return controller;
    }

    protected override IEnumerable<Profile> GetProfiles() => [new ActionProfile(), new Gebhardt.Acc.TransportOrder.WebApi.Profiles.ActionProfile()];
    
    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> AddActionGroup(ActionGroupReadModel actionGroup)
    {
        _actionGroupReadModels.Add(actionGroup);
        return this;
    }

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> AddActionGroup(Common.Messages.ActionGroups.ActionGroup actionGroup)
    {
        _actionGroups.Add(Mapper.Map<TransportOrder.Domain.Models.ActionGroup.ActionGroup>(actionGroup));
        return this;
    }

    public new ActionGroupControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();

        return this;
    }
}