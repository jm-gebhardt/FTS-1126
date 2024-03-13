using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Configurations;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

public abstract class ActionGroupTestFixtureBase<T> : MassTransitConsumerTestFixture<T> where T : class, IConsumer
{
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new();

    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalService = new();

    protected Mock<IRouteService> RouteService { get; } = new();

    protected Mock<IActionGroupRepository> ActionGroupRepository { get; } = new();

    protected Mock<IActionGroupDurationEstimationService> ActionGroupDurationEstimationService { get; } = new();

    public readonly AgvSettings Settings = new();
    private readonly AgvSettings _agvSettings = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => ActionGroupRepository.Object);
        collection.AddTransient(_ => ActionGroupDurationEstimationService.Object);
        collection.AddTransient(_ => RouteService.Object);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _actionGroupValidationService.Object);
        collection.AddFakeSettings(() => _agvSettings);
        collection.AddTransient(_ => _actionGroupRetrievalService.Object);
    }

    protected ActionGroupTestFixtureBase(TimeSpan? inactivityTimeout = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        params Action<IServiceCollection>[] services) : base(busRegistrationConfigurator, inMemoryConfiguration, services)
    {
        _agvSettings = Settings;
        Build();
        if (inactivityTimeout != null)
        {
            TestHarness.TestInactivityTimeout = (TimeSpan)inactivityTimeout;
        }
        ActionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?)null);
        ActionGroupRepository.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).Verifiable();
    }

    protected ActionGroupTestFixtureBase(TimeSpan? inactivityTimeout = null)
    {
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
        _agvDb.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync((AgvData?)null);
        _agvDb.AddAgvsToMockRepository(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        _agvDb.AddAgvsToMockRepository(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        return this;
    }

    public virtual ActionGroupTestFixtureBase<T> AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction = true, bool hasNotStartedAction = true, bool existsPriorToTest = true)
    {
        ActionGroupRepository.Setup(x => x.UpdateAsync(It.IsAny<ActionGroup>())).Verifiable();
        ActionGroupRepository.Setup(x => x.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<ActionState>(), It.IsAny<Reason>())).ReturnsAsync(actionGroup);
        ActionGroupRepository.Setup(x => x.GetSubsequentActionAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());
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
        _agvDb.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync((AgvData?)null);
        RouteService.Setup(x => x.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<string>(), null, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Route(1f, nodes));
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

    public static IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile(), new AgvProfile());
        return mapper;
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