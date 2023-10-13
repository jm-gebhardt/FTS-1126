using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
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

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Agv;

public class AgvsControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, AgvsController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    public string[] AgvIds { get; } = {"AGV001", "AGV002", "AGV003"};

    // ReSharper disable once MemberCanBePrivate.Global
    public string[] Orders { get; } = {"ORDER001", "ORDER002", ""};

    public AgvState[] States { get; } = { AgvState.Idle, AgvState.Busy, AgvState.Discovered };

    private string[] Ips { get; } = {"127.0.0.1", "127.0.0.2", "127.0.0.3"};

    private Mock<IAgvRepository> AgvRegistry { get; }

    private Mock<IAgvDataRegistry> AgvDataRegistry { get; }

    private Mock<IActionGroupRegistry> ActionGroupRegistry { get; }

    private Mock<IActionGroupRetrievalService> ActionGroupRetrievalService { get; }

    private Mock<IAgvGraphRegistry> AgvGraphRegistry { get; }

    private Mock<IAgvErrorRegistry> AgvErrorRegistry { get; }

    private Mock<IRequestClient<QueueActionGroup>> QueueActionGroupRequestClient { get; set; }

    private readonly GraphNode[] _nodes =
    {
        new("node00") {X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node10") {X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node01") {X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node11") {X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint}
    };

    public Common.Messages.ActionGroups.ActionGroup? SentQueueActionGroup { [UsedImplicitly]
        get; private set; }

    public AgvsControllerTestFixture()
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
    public AgvsControllerTestFixture<TRequest, TRequestHandler> SetupAgvRegistryWithDefaultData()
    {
        var data = new Dictionary<Guid, AgvData>();
        List<Domain.Models.Agv.Agv> agvs = new();
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

        QueueActionGroupRequestClient = new Mock<IRequestClient<QueueActionGroup>>();
        QueueActionGroupRequestClient.Setup(x => x.GetResponse<Success>(It.IsAny<QueueActionGroup>(), default, default))
            .Callback((QueueActionGroup q, CancellationToken _, RequestTimeout _) => { SentQueueActionGroup = q.ActionGroup; });

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
    public AgvsControllerTestFixture<TRequest, TRequestHandler> AddService(Action<IServiceCollection> serviceAction)
    {
        AddServices(serviceAction);
        return this;
    }

    /// <inheritdoc/>
    protected override AgvsController GetController()
    {
        IRequestClient<GetAgvs> getAgvsRequestClient = GetRequestClient<GetAgvs>();
        IRequestClient<GetAgvRoutes> getAgvRoutesRequestClient = GetRequestClient<GetAgvRoutes>();
        IRequestClient<GetAgvPositions> getAgvPositionsRequestClient = GetRequestClient<GetAgvPositions>();
        IRequestClient<GetAgvGraphs> getAgvGraphsRequestClient = GetRequestClient<GetAgvGraphs>();
        IRequestClient<GetAgvsData> getAgvGetAgvDataRequestClient = GetRequestClient<GetAgvsData>();
        IRequestClient<GetActiveErrorsForAgvs> getActiveErrorsRequestClient = GetRequestClient<GetActiveErrorsForAgvs>();
        IRequestClient<GetErrorHistoryForAgvs> getErrorHistoryRequestClient = GetRequestClient<GetErrorHistoryForAgvs>();

        var controller = new AgvsController(GetMapper(),
            getAgvsRequestClient,
            getAgvRoutesRequestClient,
            getAgvPositionsRequestClient, 
            getAgvGraphsRequestClient,
            getAgvGetAgvDataRequestClient,
            getActiveErrorsRequestClient,
            getErrorHistoryRequestClient)
        {
            ControllerContext = new ControllerContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ControllerActionDescriptor()))
        };

        return controller;
    }

    /// <summary>
    /// Uses the default services
    /// </summary>
    /// <returns></returns>
    public AgvsControllerTestFixture<TRequest, TRequestHandler> UseDefaultServices()
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
        });

        return this;
    }

    public AgvsControllerTestFixture<TRequest, TRequestHandler> WithActiveAgvErrors(IList<AgvError> agvErrors)
    {
        AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(agvErrors);
        AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(agvErrors);
        return this;
    }

    public new AgvsControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();

        return this;
    }
}