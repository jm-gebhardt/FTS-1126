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
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.AgvPositionStatus;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.GraphNode;
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

    private readonly Mock<IMapRegistry> _mapRegistry = new();

    private readonly ProductionOptions _productionOptions = new() { AutoAccept = true };

    public string[] AgvIds { get; } = {"AGV001", "AGV002", "AGV003"};

    // ReSharper disable once MemberCanBePrivate.Global
    public string[] Orders { get; } = {"ORDER001", "ORDER002", ""};

    private AgvState[] States { get; } = { AgvState.Idle, AgvState.Busy, AgvState.Discovered };

    public string[] Ips { get; } = {"127.0.0.1", "127.0.0.2", "127.0.0.3"};

    public Mock<IAgvRepository> AgvRegistry { get; }

    private Mock<IAgvDataRegistry> AgvDataRegistry { get; }

    private Mock<IActionGroupRegistry> ActionGroupRegistry { get; }

    private Mock<IActionGroupRetrievalService> ActionGroupRetrievalService { get; }

    private Mock<IAgvGraphRegistry> AgvGraphRegistry { get; }

    private Mock<IAgvErrorRegistry> AgvErrorRegistry { get; }

    private Mock<IRequestClient<QueueActionGroup>> QueueActionGroupRequestClient { get; }

    private readonly GraphNode[] _nodes =
    {
        new("node00") {X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node10") {X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node01") {X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node11") {X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint}
    };

    public AgvControllerTestFixture()
    {
        AgvGraphRegistry = new Mock<IAgvGraphRegistry>();
        AgvErrorRegistry = new Mock<IAgvErrorRegistry>();
        AgvRegistry = new Mock<IAgvRepository>();
        QueueActionGroupRequestClient = new Mock<IRequestClient<QueueActionGroup>>();
        AgvDataRegistry = new Mock<IAgvDataRegistry>();
        ActionGroupRetrievalService = new Mock<IActionGroupRetrievalService>();
        ActionGroupRetrievalService.Setup(r => r.GetAssignedActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>())).ReturnsAsync(() => AgvIds.ToDictionary(GetGuid, _ => (ActionGroupReadModel?) null));

        ActionGroupRegistry = new Mock<IActionGroupRegistry>();
        ActionGroupRegistry.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(() => AgvIds.ToDictionary(GetGuid, _ => (Domain.Models.ActionGroup.ActionGroup?) null));
        AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        ActionGroupRegistry.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(() => AgvIds.ToDictionary(GetGuid, _ => (Domain.Models.ActionGroup.ActionGroup?) null));
    }

    [UsedImplicitly]
    public AgvControllerTestFixture<TRequest, TRequestHandler> SetupAgvRegistryWithDefaultData()
    {
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv>();
        var data = new Dictionary<Guid, AgvData>();

        for (int i = 0; i < AgvIds.Length; i++)
        {
            string agvId = AgvIds[i];
            string orderId = Orders[i];
            var state = States[i];
            string ip = Ips[i];
            var correlationId = GetGuid(agvId);
            var agvStatus = agvId == "AGV003" ? null : new Domain.Models.Agv.AgvStatus(correlationId, orderId, true, true, 10, true, true, lastNodeId: "Node");
            var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());
            var agv = new Domain.Models.Agv.Agv(correlationId, agvId, agvStatus);
            agv = AgvGenerator.SetAgvState(state, false, agv);

            agvs.Add(agv);
            data[correlationId] = agvData;
        }

        AgvRegistry.AddAgvsToMockRepository(agvs);

        AgvDataRegistry.Setup(x => x.Exists(It.IsAny<Guid>())).Returns<Guid>(correlationId => data.ContainsKey(correlationId));
        AgvDataRegistry.Setup(x => x.GetAgvData(It.IsAny<Guid>()))
            .Returns<Guid>(correlationId =>
            {
                data.TryGetValue(correlationId, out var agvData);
                return agvData;
            });
        AgvDataRegistry.Setup(x => x.AddOrUpdateAsync(It.IsAny<AgvData>()))
            .ReturnsAsync((AgvData agvData) =>
            {
                data[agvData.CorrelationId] = agvData;
                return true;
            });

        return this;
    }

    private static Guid GetGuid(string agvId) => agvId.ToGuid();

    private Mock<IAgvPositionRegistry> GetAgvPositionRegistry()
    {
        var registry = new Mock<IAgvPositionRegistry>();
        var agvPositions = new List<AgvPositionStatus>();

        for (int i = 0; i < AgvIds.Length; i++)
        {
            string agvId = AgvIds[i];
            var correlationId = GetGuid(agvId);
            var node = _nodes[i];

            agvPositions.Add(new AgvPositionStatus(correlationId, agvId, new AgvPosition(true, node.X, node.Y, node.Z, 0.5f)));
        }

        registry.Setup(x => x.GetAllPositions()).Returns(agvPositions);
        registry.Setup(x => x.GetPosition(It.IsAny<Guid>())).Returns<Guid>(correlationId => { return agvPositions.FirstOrDefault(agvPosition => agvPosition.CorrelationId == correlationId); });

        return registry;
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
        IRequestClient<GetAgvRoute> getAgvRouteRequestClient = GetRequestClient<GetAgvRoute>();
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

        var controller = new AgvController(GetMapper(),
            getAgvRequestClient,
            getAgvGraphRequestClient,
            getAgvDataRequestClient,
            getAgvRouteRequestClient,
            getAgvPositionRequestClient,
            acceptAgvRequestClient,
            unlockAgvRequestClient,
            lockAgvRequestClient,
            emitRequestDataRequestClient,
            emitRequestGraphRequestClient,
            getAgvErrorsRequestClient,
            getAgvErrorHistoryRequestClient,
            moveAgvRequestClient,
            setAgvForceIdleClient)
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
        AgvGraphRegistry.Setup(x => x.GetAgvGraphAsync(correlationId)).ReturnsAsync(agvGraph);
        return this;
    }

    /// <summary>
    /// Uses the default services
    /// </summary>
    /// <returns></returns>
    public AgvControllerTestFixture<TRequest, TRequestHandler> UseDefaultServices()
    {
        SetupAgvRegistryWithDefaultData();
        AddServices(sc =>
        {
            sc.AddTransient(_ => AgvRegistry.Object);
            sc.AddTransient(_ => ActionGroupRetrievalService.Object);
            sc.AddTransient(_ => AgvDataRegistry.Object);
            sc.AddTransient(_ => GetAgvPositionRegistry().Object);
            sc.AddTransient(_ => GetMapper());
            sc.AddTransient(_ => new Mock<IBus>().Object);
            sc.AddTransient(_ => new Mock<IAgvPositionRegistry>().Object);
            sc.AddTransient(_ => ActionGroupRegistry.Object);
            sc.AddTransient(_ => AgvGraphRegistry.Object);
            sc.AddTransient(_ => AgvErrorRegistry.Object);
            sc.AddTransient(_ => QueueActionGroupRequestClient.Object);
            sc.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
            sc.AddTransient(_ => _settingsProvider.Object);
            sc.AddTransient(_ => _mapRegistry.Object);
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