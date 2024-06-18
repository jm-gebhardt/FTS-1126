using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

public abstract class ActionGroupTestFixtureBase<T> : MassTransitConsumerTestFixture<T> where T : class, IConsumer
{
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new();

    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalService = new();
    private readonly Mock<IMetricCollectionService> _metricCollectorServiceMock = new();

    protected Mock<IRouteService> RouteService { get; } = new();

    public Mock<IActionGroupRepository> ActionGroupRepository { get; } = new();

    protected Mock<IActionGroupDurationEstimationService> ActionGroupDurationEstimationService { get; } = new();

    protected override IEnumerable<Profile> GetProfiles() => [new ActionProfile(), new AgvProfile()];

    public readonly AgvSettingsReadModel SettingsReadModel = new();

    protected override MassTransitConsumerTestFixture<T> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => ActionGroupRepository.Object);
        collection.AddTransient(_ => ActionGroupDurationEstimationService.Object);
        collection.AddTransient(_ => RouteService.Object);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _actionGroupValidationService.Object);
        collection.AddTransient(_ => _actionGroupRetrievalService.Object);
        collection.AddTransient(_ => _metricCollectorServiceMock.Object);
        collection.AddTransient<ActionCommandHelper>();
        _metricCollectorServiceMock.Setup(x => x.CollectOrderCompletionMetrics(It.IsAny<ActionGroup>(), It.IsAny<string?>())).Verifiable();
        return this;
    }

    public void VerifyOrderCompletionMetricsCollected(Times callTimes)
    {
        _metricCollectorServiceMock.Verify(x => x.CollectOrderCompletionMetrics(It.IsAny<ActionGroup>(), It.IsAny<string?>()), callTimes);
    }

    public void VerifyGetNextManualActionGroupForAgvAsyncInput(Guid agvCorrelationId, List<ActionType>? allowedActionTypes = null)
    {
        if (allowedActionTypes == null)
        {
            ActionGroupRepository.Verify(x => x.GetNextManualActionGroupForAgvAsync(
                    It.Is<Guid>(g => g == agvCorrelationId),
                    It.Is<List<ActionType>?>(a => a == null)),
                Times.Once);
            return;
        }
        ActionGroupRepository.Verify(x => x.GetNextManualActionGroupForAgvAsync(
                It.Is<Guid>(g => g == agvCorrelationId),
                It.Is<List<ActionType>>(a => a.SequenceEqual(allowedActionTypes))),
            Times.Once);
    }

   protected ActionGroupTestFixtureBase(TimeSpan? inactivityTimeout = null)
    {
        _agvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(SettingsReadModel);

        Build();
        if (inactivityTimeout != null)
        {
            TestHarness.TestInactivityTimeout = (TimeSpan)inactivityTimeout;
        }
        ActionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?)null);
        ActionGroupRepository.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).Verifiable();
    }

    public ActionGroupTestFixtureBase<T> AddAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvData(AgvDataGenerator.GenerateAgvData(correlationId:agv.CorrelationId));
        _agvDb.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        _chooseAgvActionGroupService.Setup(x => x.GetAgvAsync(It.IsAny<ActionGroup>(), It.IsAny<IList<TransportOrder.Domain.Models.Agv.Agv>>(), It.IsAny<IList<AgvData>>(), It.IsAny<CancellationToken>())).ReturnsAsync(agv);
        return this;
    }

    public virtual ActionGroupTestFixtureBase<T> AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction = true, bool hasNotStartedAction = true, bool existsPriorToTest = true)
    {
        ActionGroupRepository.Setup(x => x.UpdateAsync(It.IsAny<ActionGroup>())).Verifiable();
        ActionGroupRepository.Setup(x => x.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<ActionState>(), It.IsAny<Reason>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.GetSubsequentActionAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.FirstOrDefault( a=> a.State == ActionState.Created) ?? actionGroup.Actions.First());
        ActionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<ActionGroupState>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>())).ReturnsAsync(new List<ActionGroup> { actionGroup });
        if (existsPriorToTest)
        {
            ActionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(new List<ActionGroup> { actionGroup });
            _chooseAgvActionGroupService.Setup(x => x.GetNotStartedActionGroupAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(actionGroup);
        }
        else
        {
            ActionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(new List<ActionGroup>());
            _chooseAgvActionGroupService.Setup(x => x.GetNotStartedActionGroupAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(null as ActionGroup);
        }
        ActionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.HasNotStartedActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasNotStartedAction);
        ActionGroupRepository.Setup(x => x.HasUnfinishedDropActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasUnfinishedAction);
        ActionGroupRepository.Setup(x => x.GetRequiredActionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());
        ActionGroupRepository.Setup(x => x.GetNextManualActionGroupForAgvAsync(It.IsAny<Guid>(), It.IsAny<List<ActionType>?>())).ReturnsAsync(actionGroup);
        return this;
    }

    public ActionGroupTestFixtureBase<T> AddActionGroupRetrieval(bool headingTo)
    {
        _actionGroupRetrievalService.Setup(x => x.IsAgvHeadingToNodeOfType(It.IsAny<Guid>(), It.IsAny<AgvGraph.NodeType>())).ReturnsAsync(headingTo);
        return this;
    }

    public ActionGroupTestFixtureBase<T> AddActionGroupTargetNodeType(ActionGroup actionGroup, AgvGraph.NodeType nodeType)
    {
        _actionGroupRetrievalService.Setup(x => x.EnrichActionGroupWithNodesAsync(It.IsAny<ActionGroup>())).ReturnsAsync(actionGroup.ConvertFromActionGroupWithGivenNodeType(nodeType)!);
        return this;
    }

    public ActionGroupTestFixtureBase<T> AddRoute(string[] nodes)
    {
        _agvDb.AddAgvData((AgvData?)null);
        RouteService.WithRoute(0f, nodes);
        return this;
    }

    public void SetupValidationResult(ValidationResult validationResult)
    {
        _actionGroupValidationService.Setup(x => x.ValidateAsync(It.IsAny<ActionGroup>())).ReturnsAsync(validationResult);
    }

    public void IsActionGroupSaved(Times howOften)
    {
        ActionGroupRepository.Verify(x => x.AddAsync(It.IsAny<ActionGroup>()), howOften);
    }

    public async Task<Response<TResult>> GetResponseAsync<TResult, TRequest>(TRequest request)
        where TResult : class
        where TRequest : class
    {
        IRequestClient<TRequest> client = TestHarness.Bus!.CreateRequestClient<TRequest>();
        Response<TResult> response = await client.GetResponse<TResult>(request);
        return response;
    }
}