using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AgvData = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvData;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;
using AgvStatus = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.AgvStatus;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Agv;

public class AgvControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, AgvController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();

    private readonly Mock<ISettingsProvider<AgvSettings>> _settingsProvider = new();

    private readonly Mock<IMapRepository> _mapRepository = new();

    private readonly ProductionOptions _productionOptions = new() {AutoAccept = true};

    public string[] AgvIds { get; } = {"AGV001", "AGV002", "AGV003"};

    // ReSharper disable once MemberCanBePrivate.Global
    public string[] Orders { get; } = {"ORDER001", "ORDER002", ""};

    private AgvState[] States { get; } = {AgvState.Idle, AgvState.Busy, AgvState.Discovered};

    public string[] Ips { get; } = {"127.0.0.1", "127.0.0.2", "127.0.0.3"};

    public Mock<IAgvRepository> AgvRepository { get; }

    private Mock<IAgvDataRepository> AgvDataRepository { get; }

    private Mock<IActionGroupRepository> ActionGroupRepository { get; }

    private Mock<IActionGroupRetrievalService> ActionGroupRetrievalService { get; }

    private Mock<IAgvGraphRepository> AgvGraphRepository { get; }

    private Mock<IAgvErrorRepository> AgvErrorRepository { get; }

    private Mock<IRequestClient<QueueActionGroup>> QueueActionGroupRequestClient { get; }

    private Mock<ICargoRepository> CargoRepository { get; }

    private readonly GraphNode[] _nodes =
    {
        new("node00") {X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node10") {X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node01") {X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node11") {X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint}
    };

    public AgvControllerTestFixture()
    {
        AgvGraphRepository = new Mock<IAgvGraphRepository>();
        AgvErrorRepository = new Mock<IAgvErrorRepository>();
        AgvRepository = new Mock<IAgvRepository>();
        QueueActionGroupRequestClient = new Mock<IRequestClient<QueueActionGroup>>();
        AgvDataRepository = new Mock<IAgvDataRepository>();
        ActionGroupRetrievalService = new Mock<IActionGroupRetrievalService>();
        ActionGroupRetrievalService.Setup(r => r.GetAssignedActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>())).ReturnsAsync(() => AgvIds.ToDictionary(GetGuid, _ => (ActionGroupReadModel?) null));

        ActionGroupRepository = new Mock<IActionGroupRepository>();
        ActionGroupRepository.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(() => AgvIds.ToDictionary(GetGuid, _ => (Domain.Models.ActionGroup.ActionGroup?) null));
        AgvErrorRepository.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        AgvErrorRepository.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        ActionGroupRepository.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(() => AgvIds.ToDictionary(GetGuid, _ => (Domain.Models.ActionGroup.ActionGroup?) null));
        CargoRepository = new Mock<ICargoRepository>();
    }

    [UsedImplicitly]
    public AgvControllerTestFixture<TRequest, TRequestHandler> SetupAgvRepositoryWithDefaultData()
    {
        var agvs = new List<Domain.Models.Agv.Agv>();
        var data = new Dictionary<Guid, AgvData>();

        for (int i = 0; i < AgvIds.Length; i++)
        {
            string agvId = AgvIds[i];
            string orderId = Orders[i];
            var state = States[i];
            string ip = Ips[i];
            var correlationId = GetGuid(agvId);
            var agvStatus = agvId == "AGV003" ? null : new AgvStatus(correlationId, orderId, true, true, 10, true, true, "Node");
            var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());
            var agv = new Domain.Models.Agv.Agv(correlationId, agvId, agvStatus);
            agv = AgvGenerator.SetAgvState(state, false, agv);

            agvs.Add(agv);
            data[correlationId] = agvData;
        }

        AgvRepository.AddAgvsToMockRepository(agvs);

        AgvDataRepository.Setup(x => x.Exists(It.IsAny<Guid>())).Returns<Guid>(correlationId => data.ContainsKey(correlationId));
        AgvDataRepository.Setup(x => x.GetAgvData(It.IsAny<Guid>()))
            .Returns<Guid>(correlationId =>
            {
                data.TryGetValue(correlationId, out var agvData);
                return agvData;
            });
        AgvDataRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<AgvData>()))
            .ReturnsAsync((AgvData agvData) =>
            {
                data[agvData.CorrelationId] = agvData;
                return true;
            });

        return this;
    }

    private static Guid GetGuid(string agvId) => agvId.ToGuid();

    private Mock<IAgvPositionRepository> GetAgvPositionRepository()
    {
        var repository = new Mock<IAgvPositionRepository>();
        var agvPositions = new List<AgvPositionStatus>();

        for (int i = 0; i < AgvIds.Length; i++)
        {
            string agvId = AgvIds[i];
            var correlationId = GetGuid(agvId);
            var node = _nodes[i];

            agvPositions.Add(new AgvPositionStatus(correlationId, agvId, new AgvPosition(true, node.X, node.Y, node.Z, 0.5f)));
        }

        repository.Setup(x => x.GetAllPositions()).Returns(agvPositions);
        repository.Setup(x => x.GetPosition(It.IsAny<Guid>())).Returns<Guid>(correlationId => { return agvPositions.FirstOrDefault(agvPosition => agvPosition.CorrelationId == correlationId); });

        return repository;
    }

    /// <summary>
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="serviceAction">The action of the service to add</param>
    /// <returns></returns>
    [UsedImplicitly]
    public AgvControllerTestFixture<TRequest, TRequestHandler> AddService(Action<IServiceCollection> serviceAction)
    {
        AddServices(serviceAction);
        return this;
    }

    /// <inheritdoc/>
    protected override AgvController GetController()
    {
        IRequestClient<GetAgv> getAgvRequestClient = GetRequestClient<GetAgv>();
        IRequestClient<GetAgvGraph> getAgvGraphRequestClient = GetRequestClient<GetAgvGraph>();
        IRequestClient<GetAgvData> getAgvDataRequestClient = GetRequestClient<GetAgvData>();
        IRequestClient<GetCurrentAgvRoute> getCurrentAgvRouteRequestClient = GetRequestClient<GetCurrentAgvRoute>();
        IRequestClient<GetAgvPosition> getAgvPositionRequestClient = GetRequestClient<GetAgvPosition>();
        IRequestClient<AcceptAgvCommand> acceptAgvRequestClient = GetRequestClient<AcceptAgvCommand>();
        IRequestClient<UnlockAgv> unlockAgvRequestClient = GetRequestClient<UnlockAgv>();
        IRequestClient<LockAgv> lockAgvRequestClient = GetRequestClient<LockAgv>();
        IRequestClient<RequestAgvData> emitRequestDataRequestClient = GetRequestClient<RequestAgvData>();
        IRequestClient<RequestAgvGraph> emitRequestGraphRequestClient = GetRequestClient<RequestAgvGraph>();
        IRequestClient<GetActiveAgvErrors> getAgvErrorsRequestClient = GetRequestClient<GetActiveAgvErrors>();
        IRequestClient<GetAgvErrors> getAgvErrorHistoryRequestClient = GetRequestClient<GetAgvErrors>();
        IRequestClient<MoveAgv> moveAgvRequestClient = GetRequestClient<MoveAgv>();
        IRequestClient<SetAgvForceIdle> setAgvForceIdleClient = GetRequestClient<SetAgvForceIdle>();
        IRequestClient<IsAgvLoadedRequest> isAgvLoadedClient = GetRequestClient<IsAgvLoadedRequest>();
        IRequestClient<GetCargosOfAgv> getCargosOfAgvClient = GetRequestClient<GetCargosOfAgv>();

        var controller = new AgvController(GetMapper(),
            getAgvRequestClient,
            getAgvGraphRequestClient,
            getAgvDataRequestClient,
            getCurrentAgvRouteRequestClient,
            getAgvPositionRequestClient,
            acceptAgvRequestClient,
            unlockAgvRequestClient,
            lockAgvRequestClient,
            emitRequestDataRequestClient,
            emitRequestGraphRequestClient,
            getAgvErrorsRequestClient,
            getAgvErrorHistoryRequestClient,
            moveAgvRequestClient,
            setAgvForceIdleClient,
            isAgvLoadedClient,
            getCargosOfAgvClient)
        {
            ControllerContext = new ControllerContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ControllerActionDescriptor()))
        };

        return controller;
    }

    public AgvControllerTestFixture<TRequest, TRequestHandler> WithAgvGraph(Guid correlationId, AgvGraph agvGraph)
    {
        AgvGraphRepository.Setup(x => x.GetAgvGraphAsync(correlationId)).ReturnsAsync(agvGraph);
        return this;
    }

    /// <summary>
    /// Uses the default services
    /// </summary>
    /// <returns></returns>
    public AgvControllerTestFixture<TRequest, TRequestHandler> UseDefaultServices()
    {
        SetupAgvRepositoryWithDefaultData();
        AddServices(sc =>
        {
            sc.AddTransient(_ => AgvRepository.Object);
            sc.AddTransient(_ => ActionGroupRetrievalService.Object);
            sc.AddTransient(_ => AgvDataRepository.Object);
            sc.AddTransient(_ => GetAgvPositionRepository().Object);
            sc.AddTransient(_ => GetMapper());
            sc.AddTransient(_ => new Mock<IBus>().Object);
            sc.AddTransient(_ => new Mock<IAgvPositionRepository>().Object);
            sc.AddTransient(_ => ActionGroupRepository.Object);
            sc.AddTransient(_ => AgvGraphRepository.Object);
            sc.AddTransient(_ => AgvErrorRepository.Object);
            sc.AddTransient(_ => CargoRepository.Object);
            sc.AddTransient(_ => QueueActionGroupRequestClient.Object);
            sc.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
            sc.AddTransient(_ => _settingsProvider.Object);
            sc.AddTransient(_ => _mapRepository.Object);
            sc.AddTransient(_ => _productionOptions);
        });

        return this;
    }

    public new AgvControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();

        return this;
    }
}