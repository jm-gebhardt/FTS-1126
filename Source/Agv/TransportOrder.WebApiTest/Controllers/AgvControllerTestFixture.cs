using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    public class AgvControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, AgvController>
        where TRequest : class
        where TRequestHandler : class, IConsumer
    {
        public string[] AgvIds { get; } = {"AGV001", "AGV002", "AGV003"};

        // ReSharper disable once MemberCanBePrivate.Global
        public string[] Orders { get; } = {"ORDER001", "ORDER002", ""};

        public string[] States { get; } = {nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Discovered)};

        public string[] Ips { get; } = {"127.0.0.1", "127.0.0.2", "127.0.0.3"};

        public Mock<IAgvRegistry> AgvRegistry { get; }

        private Mock<IAgvDataRegistry> AgvDataRegistry { get; }

        private Mock<IActionGroupRegistry> ActionGroupRegistry { get; }

        private Mock<IAgvGraphRegistry> AgvGraphRegistry { get; }

        private Mock<IAgvErrorRegistry> AgvErrorRegistry { get; }

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
            AgvRegistry = new Mock<IAgvRegistry>();
            AgvDataRegistry = new Mock<IAgvDataRegistry>();
            ActionGroupRegistry = new Mock<IActionGroupRegistry>();
            ActionGroupRegistry.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(() => AgvIds.ToDictionary(GetGuid, _ => (ActionGroup?) null));
            AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
            AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        }

        [UsedImplicitly]
        public AgvControllerTestFixture<TRequest, TRequestHandler> SetupAgvRegistryWithDefaultData()
        {
            var agvs = new List<IAgv>();
            var data = new Dictionary<Guid, AgvData>();

            for (int i = 0; i < AgvIds.Length; i++)
            {
                string agvId = AgvIds[i];
                string orderId = Orders[i];
                string state = States[i];
                string ip = Ips[i];
                var correlationId = GetGuid(agvId);
                var agvStatus = agvId == "AGV003" ? null : new AgvStatus(correlationId, orderId, true, true, 10, true, true);
                var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());

                agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus));
                data[correlationId] = agvData;
            }

            AgvRegistry.SetupGet(x => x.Count).Returns(agvs.Count);
            AgvRegistry.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
            AgvRegistry.Setup(x => x.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
            AgvRegistry.Setup(x => x.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(agvs));
            AgvRegistry.Setup(x => x.GetAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId => { return agvs.FirstOrDefault(agv => agv.CorrelationId == correlationId); });
            AgvRegistry.Setup(x => x.GetRequiredAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId => { return agvs.First(agv => agv.CorrelationId == correlationId); });

            AgvDataRegistry.Setup(x => x.Exists(It.IsAny<Guid>())).Returns<Guid>(correlationId => data.ContainsKey(correlationId));
            AgvDataRegistry.Setup(x => x.GetAgvData(It.IsAny<Guid>()))
                .Returns<Guid>(correlationId =>
                {
                    data.TryGetValue(correlationId, out var agvData);
                    return agvData;
                });
            AgvDataRegistry.Setup(x => x.AddOrUpdate(It.IsAny<AgvData>()))
                .Returns<AgvData>(agvData =>
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
            IRequestClient<GetAgvsRequest> getAgvsRequestClient = GetRequestClient<GetAgvsRequest>();
            IRequestClient<GetAgvGraphRequest> getAgvGraphRequestClient = GetRequestClient<GetAgvGraphRequest>();
            IRequestClient<GetAgvDataRequest> getAgvDataRequestClient = GetRequestClient<GetAgvDataRequest>();
            IRequestClient<GetAgvPositionsRequest> getAgvPositionsRequestClient = GetRequestClient<GetAgvPositionsRequest>();
            IRequestClient<UnlockAgvCommand> setIdleRequestClient = GetRequestClient<UnlockAgvCommand>();
            IRequestClient<LockAgvCommand> lockAgvRequestClient = GetRequestClient<LockAgvCommand>();
            IRequestClient<RequestAgvDataCommand> emitRequestDataRequestClient = GetRequestClient<RequestAgvDataCommand>();
            IRequestClient<RequestAgvGraphCommand> emitRequestGraphRequestClient = GetRequestClient<RequestAgvGraphCommand>();
            IRequestClient<GetActiveAgvErrorsRequest> getAgvErrorsRequestClient = GetRequestClient<GetActiveAgvErrorsRequest>();
            IRequestClient<GetAgvErrorsRequest> getAgvErrorHistoryRequestClient = GetRequestClient<GetAgvErrorsRequest>();

            var controller = new AgvController(getAgvsRequestClient, getAgvGraphRequestClient, getAgvDataRequestClient,
                getAgvPositionsRequestClient, setIdleRequestClient, lockAgvRequestClient, emitRequestDataRequestClient, emitRequestGraphRequestClient, getAgvErrorsRequestClient, getAgvErrorHistoryRequestClient)
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
                sc.AddTransient(_ => AgvDataRegistry.Object);
                sc.AddTransient(_ => GetAgvPositionRegistry().Object);
                sc.AddTransient(_ => GetMapper());
                sc.AddTransient(_ => new Mock<IBus>().Object);
                sc.AddTransient(_ => new Mock<IAgvPositionRegistry>().Object);
                sc.AddTransient(_ => ActionGroupRegistry.Object);
                sc.AddTransient(_ => AgvErrorRegistry.Object);
                sc.AddTransient(_ => AgvGraphRegistry.Object);
            });
            return this;
        }

        public AgvControllerTestFixture<TRequest, TRequestHandler> WithActiveAgvErrors(IList<AgvError> agvErrors)
        {
            AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(agvErrors);
            AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(agvErrors);
            return this;
        }

        public new AgvControllerTestFixture<TRequest, TRequestHandler> Build()
        {
            base.Build();
            return this;
        }
    }
}