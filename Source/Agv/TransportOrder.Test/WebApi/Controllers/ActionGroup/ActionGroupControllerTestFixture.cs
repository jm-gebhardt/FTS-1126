using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.RequestHandlers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.ActionGroup;

public class ActionGroupControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, ActionGroupController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private readonly Mock<IActionGroupRetrievalService> _retrievalServiceMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepositoryMock = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IRouteService> _routeService = new();
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IAgvsSemaphore>>>().Object);
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IGraphRepository> _graphRepositoryMock = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();
    private readonly List<ActionGroupReadModel> _actionGroupReadModels = new();
    private readonly List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> _actionGroups = new();
    private readonly Mock<IActionGroupDurationEstimationService> _actionGroupDurationEstimationService = new();
    private readonly Mock<IQueueManager> _queueManager = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<INodeRepository> _nodeRepository = new();
    
    public ActionGroupControllerTestFixture()
    {
        _agvDb.SetupNodeFallbacks();
        AddConsumers<SuccessRequestHandlerMock<AbortActionGroup>>();
        AddConsumers<SuccessRequestHandlerMock<ResetActionGroup>>();

        AddServices(sc =>
        {
            sc.AddTransient(_ => _retrievalServiceMock.Object);
            sc.AddTransient(_ => _actionGroupRepositoryMock.Object);
            sc.AddTransient(_ => _stationRepository.Object);
            sc.AddTransient(_ => _actionGroupDurationEstimationService.Object);
            sc.AddTransient(_ => _routeService.Object);
            sc.AddTransient(_ => _nodeRepository.Object);
            sc.AddTransient(_ => _chooseAgvActionGroupService.Object);
            sc.AddTransient(_ => _maintenanceService.Object);
            sc.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
            sc.AddTransient(_ => _agvDb.Object);
            sc.AddTransient(_ => _graphRepositoryMock.Object);
            sc.AddTransient(_ => _actionGroupValidationService.Object);
            sc.AddTransient<ActionCommandHelper>();
            sc.AddTransient(_ => _queueManager.Object);
            sc.AddTransient(_ => _logEntryRepository.Object);
            sc.AddTransient<ILoggingService, LoggingService>();
        });
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetRequiredStationBaseInformation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _retrievalServiceMock.Setup(x => x.GetActionGroupWithNodesAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actionGroupReadModels.FirstOrDefault(a => a.CorrelationId == c));
        _actionGroupRepositoryMock.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
        {
            var ag = _actionGroups.FirstOrDefault(a => a.CorrelationId == c);
            return ag ?? throw new ActionGroupNotFoundException(c);
        });
        _actionGroupRepositoryMock.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actionGroups.FirstOrDefault(a => a.CorrelationId == c));
        _actionGroupRepositoryMock.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<ActionState>(), It.IsAny<Reason?>(), It.IsAny<DateTimeOffset>()))!.ReturnsAsync((AgvAction action, TransportOrder.Domain.Models.Agv.Agv _, ActionState _, Reason? _, DateTimeOffset _) => _actionGroups.FirstOrDefault(a => a.CorrelationId == action.ParentId));
        _retrievalServiceMock.Setup(x => x.EnrichActionGroupWithNodesAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>())).ReturnsAsync((TransportOrder.Domain.Models.ActionGroup.ActionGroup ag) => ag.ConvertFromActionGroupWithFakeNodeType()!);
        _graphRepositoryMock.Setup(x => x.NodeExistsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(true);
    }

    protected override ActionGroupController GetController()
    {
        var getActionGroupRequestClient = GetRequestClient<GetActionGroup>();
        var abortionRequestClient = GetRequestClient<TriggerActionGroupAbortion>();
        var cancelActionGroupRequestClient = GetRequestClient<CancelActionGroup>();
        var triggerResetRequestClient = GetRequestClient<TriggerActionGroupReset>();
        var createActionGroupRequestClient = GetRequestClient<CreatePickDropActionGroup>();
        var reassignActionGroupClient = GetRequestClient<TryReassignActionGroup>();
        var actionCompletionClient = GetRequestClient<TriggerActionCompletion>();
        var actionGroupQueueingClient = GetRequestClient<TriggerActionGroupQueueing>();
        var setTargetClient = GetRequestClient<TriggerSetTarget>();

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

    protected override IEnumerable<Profile> GetProfiles() => [new ActionProfile(), new Gebhardt.Acc.TransportOrder.WebApi.Profiles.ActionProfile(), new LogEntryProfile()];

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> AddActionGroup(ActionGroupReadModel actionGroup)
    {
        _actionGroupReadModels.Add(actionGroup);
        return this;
    }

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> AddActionGroup(Common.Messages.ActionGroups.ActionGroup actionGroup)
    {
        var domainActionGroup = Mapper.Map<TransportOrder.Domain.Models.ActionGroup.ActionGroup>(actionGroup);
        _actionGroups.Add(domainActionGroup);
        var graphNodes = domainActionGroup.Actions.Select(a => a.Target.Node).ToArray();
        return WithNode(graphNodes);
    }

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> WithNode(GraphNode?[] graphNodes)
    {
        _agvDb.AddNode(graphNodes);

        foreach (var node in graphNodes)
        {
            if (node != null)
            {
                _graphRepositoryMock.Setup(x => x.GetNodeAsync(node.Id)).ReturnsAsync(node);
                _graphRepositoryMock.Setup(x => x.TryGetNodeByIdAsync(node.Id)).ReturnsAsync(node);
                _graphRepositoryMock.Setup(x => x.GetNodeAsync(node.Id)).ReturnsAsync(node);
                _graphRepositoryMock.Setup(x => x.TryGetNodeByIdAsync(node.Id)).ReturnsAsync(node);
            }
        }

        return this;
    }

    public new ActionGroupControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();

        return this;
    }
}