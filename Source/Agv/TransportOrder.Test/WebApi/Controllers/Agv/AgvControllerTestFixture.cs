using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AgvData = Gebhardt.Acc.Common.Domain.Models.AgvData;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using ResetAgvErrors = Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands.ResetAgvErrors;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Agv;

public class AgvControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, AgvController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new();

    private readonly Mock<IMapRepository> _mapRepository = new();

    private readonly ProductionOptions _productionOptions = new() { AutoAccept = true };

    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();

    private readonly Mock<IParkPointService> _parkPointService = new();

    public string[] AgvSerialNumbers { get; } = { "AGV001", "AGV002", "AGV003" };

    // ReSharper disable once MemberCanBePrivate.Global
    public string[] Orders { get; } = { "ORDER001", "ORDER002", "" };

    private AgvState[] States { get; } = { AgvState.Idle, AgvState.Busy, AgvState.Discovered };

    public string[] Ips { get; } = { "127.0.0.1", "127.0.0.2", "127.0.0.3" };

    public Mock<IAgvDb> AgvDb { get; }

    private Mock<IActionGroupRepository> ActionGroupRepository { get; }

    private Mock<IActionGroupRetrievalService> ActionGroupRetrievalService { get; }

    private Mock<IRequestClient<QueueActionGroup>> QueueActionGroupRequestClient { get; }

    private Mock<IChargingPointManager> ChargingPointManager { get; }

    private readonly GraphNode[] _nodes =
    {
        new("node00") { X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
        new("node10") { X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
        new("node01") { X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
        new("node11") { X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint }
    };

    private readonly IAgvPositionRepository _agvPositionRepository;

    public AgvSettingsReadModel AgvSettingsReadModel { get; } = EfAgvSettingsProvider.DefaultSettings;

    private readonly TransportOrder.Domain.Models.ActionGroup.AgvAction _chargeAction = new(Guid.NewGuid(), ActionType.Charge, "Charger-1", Guid.NewGuid());

    public AgvControllerTestFixture()
    {
        // _chargingPointManager.GetBestChargerAsync(agv, nodeId);
        AgvDb = new Mock<IAgvDb>();
        QueueActionGroupRequestClient = new Mock<IRequestClient<QueueActionGroup>>();
        ActionGroupRetrievalService = new Mock<IActionGroupRetrievalService>();
        ActionGroupRetrievalService.Setup(r => r.GetCurrentActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>())).ReturnsAsync(() => AgvSerialNumbers.ToDictionary(GetGuid, _ => (ActionGroupReadModel?)null));
        _agvPositionRepository = GetAgvPositionRepository().Object;

        ActionGroupRepository = new Mock<IActionGroupRepository>();
        ActionGroupRepository.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(() => AgvSerialNumbers.ToDictionary(GetGuid, _ => (TransportOrder.Domain.Models.ActionGroup.ActionGroup?)null));
        ActionGroupRepository.Setup(r => r.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(_chargeAction);
        AgvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        AgvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        AgvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(AgvSettingsReadModel);
        ChargingPointManager = new Mock<IChargingPointManager>();
        ChargingPointManager.Setup(c => c.GetBestChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<string>())).ReturnsAsync(new GraphNode("Charger-1"));
        ChargingPointManager.Setup(c => c.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<string>())).ReturnsAsync(new GraphNode("Charger-1"));
    }

    [UsedImplicitly]
    public AgvControllerTestFixture<TRequest, TRequestHandler> SetupAgvDbWithDefaultData()
    {
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv>();
        var data = new Dictionary<Guid, AgvData>();

        for (int i = 0; i < AgvSerialNumbers.Length; i++)
        {
            string agvSerialNumber = AgvSerialNumbers[i];
            string orderId = Orders[i];
            var state = States[i];
            string ip = Ips[i];
            var correlationId = GetGuid(agvSerialNumber);
            var agvStatus = agvSerialNumber == "AGV003" ? null : new AgvStatus(correlationId, orderId, true, true, 10, true, true, "WAY3");
            var agvData = AgvDataGenerator.GenerateAgvData(correlationId: correlationId, ipV4Address: ip);
            var agv = new TransportOrder.Domain.Models.Agv.Agv(correlationId, Guid.NewGuid(), agvSerialNumber, agvStatus);
            agv = AgvGenerator.SetAgvState(state, agv, false);

            agvs.Add(agv);
            data[correlationId] = agvData;
        }

        AgvDb.AddAgvsToMockRepository(agvs);
        AgvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        AgvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        AgvDb.Setup(x => x.Data.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid correlationId) => data.ContainsKey(correlationId));
        AgvDb.Setup(x => x.Agvs.ThrowIfAgvNotExistsAsync(It.IsAny<Guid>()))
            .Callback((Guid correlationId) =>
            {
                if (agvs.FirstOrDefault(a => a.CorrelationId == correlationId) == null)
                {
                    throw new AgvNotFoundException(correlationId);
                }
            });
        AgvDb.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid correlationId) =>
            {
                data.TryGetValue(correlationId, out var agvData);
                return agvData;
            });
        AgvDb.Setup(x => x.Data.AddOrUpdateAsync(It.IsAny<AgvData>()))
            .Callback((AgvData agvData) => { data[agvData.CorrelationId] = agvData; });
        return this;
    }

    public AgvControllerTestFixture<TRequest, TRequestHandler> SetupAgvDbWitNode(GraphNode node)
    {
        AgvDb.Setup(x => x.Graphs.GetNodeAsync(It.IsAny<string>())).ReturnsAsync(node);
        return this;
    }

    private static Guid GetGuid(string agvSerialNumber) => agvSerialNumber.ToGuid();

    private Mock<IAgvPositionRepository> GetAgvPositionRepository()
    {
        var repository = new Mock<IAgvPositionRepository>();
        var agvPositions = new List<AgvPositionStatus>();

        for (int i = 0; i < AgvSerialNumbers.Length; i++)
        {
            string agvSerialNumber = AgvSerialNumbers[i];
            var correlationId = GetGuid(agvSerialNumber);
            var node = _nodes[i];

            agvPositions.Add(new AgvPositionStatus(correlationId, agvSerialNumber, new AgvPosition(true, node.X, node.Y, node.Z, 0.5f)));
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
    public AgvControllerTestFixture<TRequest, TRequestHandler> AddService(Action<IServiceCollection> serviceAction)
    {
        AddServices(serviceAction);
        return this;
    }

    [PublicAPI]
    public AgvControllerTestFixture<TRequest, TRequestHandler> WithHasCargo(Guid agvCorrelationId, bool hasCargo)
    {
        AgvDb.Setup(x => x.Cargos.HasAgvCargoAsync(agvCorrelationId)).ReturnsAsync(hasCargo);
        return this;
    }

    /// <inheritdoc/>
    protected override AgvController GetController()
    {
        IRequestClient<GetAgv> getAgvRequestClient = GetRequestClient<GetAgv>();
        IRequestClient<GetAgvGraph> getAgvGraphRequestClient = GetRequestClient<GetAgvGraph>();
        IRequestClient<GetAgvData> getAgvDataRequestClient = GetRequestClient<GetAgvData>();
        IRequestClient<GetAgvBattery> getAgvBatteryRequestClient = GetRequestClient<GetAgvBattery>();
        IRequestClient<GetCurrentAgvRoute> getCurrentAgvRouteRequestClient = GetRequestClient<GetCurrentAgvRoute>();
        IRequestClient<GetRoute> getRouteRequestClient = GetRequestClient<GetRoute>();
        IRequestClient<GetAgvPosition> getAgvPositionRequestClient = GetRequestClient<GetAgvPosition>();
        IRequestClient<AcceptAgvCommand> acceptAgvRequestClient = GetRequestClient<AcceptAgvCommand>();
        IRequestClient<UnlockAgv> unlockAgvRequestClient = GetRequestClient<UnlockAgv>();
        IRequestClient<LockAgv> lockAgvRequestClient = GetRequestClient<LockAgv>();
        IRequestClient<RequestAgvData> emitRequestDataRequestClient = GetRequestClient<RequestAgvData>();
        IRequestClient<RequestAgvGraph> emitRequestGraphRequestClient = GetRequestClient<RequestAgvGraph>();
        IRequestClient<GetActiveAgvErrors> getAgvErrorsRequestClient = GetRequestClient<GetActiveAgvErrors>();
        IRequestClient<GetAgvErrors> getAgvErrorHistoryRequestClient = GetRequestClient<GetAgvErrors>();
        IRequestClient<MoveAgv> moveAgvRequestClient = GetRequestClient<MoveAgv>();
        IRequestClient<ChargeAgv> chargeAgvRequestClient = GetRequestClient<ChargeAgv>();
        IRequestClient<SetAgvForceIdle> setAgvForceIdleClient = GetRequestClient<SetAgvForceIdle>();
        IRequestClient<GetAgvHasCargo> getAgvHasCargoClient = GetRequestClient<GetAgvHasCargo>();
        IRequestClient<GetCargosOfAgv> getCargosOfAgvClient = GetRequestClient<GetCargosOfAgv>();
        IRequestClient<ResetAgvErrors> resetAgvErrorsClient = GetRequestClient<ResetAgvErrors>();
        IRequestClient<GetAgvSettings> getAgvSettingsRequestClient = GetRequestClient<GetAgvSettings>();
        IRequestClient<SendAgvToCharger> sendAgvToChargerClient = GetRequestClient<SendAgvToCharger>();
        IRequestClient<UpdateAgvSettings> updateAgvSettingsClient = GetRequestClient<UpdateAgvSettings>();
        IRequestClient<ResetAgvSettingToDefault> resetAgvSettingsClient = GetRequestClient<ResetAgvSettingToDefault>();

        var controller = new AgvController(Mapper,
            getAgvRequestClient,
            getAgvGraphRequestClient,
            getAgvDataRequestClient,
            getAgvBatteryRequestClient,
            getCurrentAgvRouteRequestClient,
            getRouteRequestClient,
            getAgvPositionRequestClient,
            acceptAgvRequestClient,
            unlockAgvRequestClient,
            lockAgvRequestClient,
            emitRequestDataRequestClient,
            emitRequestGraphRequestClient,
            getAgvErrorsRequestClient,
            getAgvErrorHistoryRequestClient,
            moveAgvRequestClient,
            chargeAgvRequestClient,
            setAgvForceIdleClient,
            getAgvHasCargoClient,
            getCargosOfAgvClient,
            resetAgvErrorsClient,
            getAgvSettingsRequestClient,
            sendAgvToChargerClient,
            updateAgvSettingsClient,
            resetAgvSettingsClient
        )
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
        AgvDb.Setup(x => x.Graphs.GetAgvGraphAsync(correlationId)).ReturnsAsync(agvGraph);
        AgvDb.Setup(x => x.Graphs.TryGetNodeAsync(It.IsAny<string>()))
            .ReturnsAsync(() => agvGraph.Nodes.First());
        return this;
    }

    public new AgvControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        _nodeStateRepository.Setup(x => x.ReserveNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).Verifiable();
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync((NodeLock?)null);

        AddConsumers<FakeQueueActionGroupConsumer>();

        AddServices(sc =>
        {
            sc.AddTransient(_ => AgvDb.Object);
            sc.AddTransient(_ => ActionGroupRetrievalService.Object);
            sc.AddTransient(_ => _agvPositionRepository);
            sc.AddTransient(_ => new Mock<IAgvPositionRepository>().Object);
            sc.AddTransient(_ => ActionGroupRepository.Object);
            sc.AddTransient(_ => QueueActionGroupRequestClient.Object);
            sc.AddTransient(_ => ChargingPointManager.Object);
            sc.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
            sc.AddTransient(_ => _mapRepository.Object);
            sc.AddTransient(_ => _productionOptions);
            sc.AddTransient(_ => _nodeStateRepository.Object);
            sc.AddTransient(_ => _parkPointService.Object);
            sc.AddTransient<IMetricCollectionService, PrometheusMetricCollectionService>();
        });
        base.Build();

        return this;
    }
}