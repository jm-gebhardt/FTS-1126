using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
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
        public string[] AgvIds { get; } = new string[] { "AGV001", "AGV002", "AGV003" };
        public string[] Orders { get;  } = new string[] { "ORDER001", "ORDER002", "" };
        public string[] States { get;  } = new string[] { nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Discovered) };
        public string[] Ips { get;  } = new string[] { "127.0.0.1", "127.0.0.2", "127.0.0.3" };

        public AgvGraph.Node[] Nodes { get; } = new AgvGraph.Node[]
        {
            new("node00") { X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node10") { X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node01") { X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node11") { X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint }
        };

        public AgvGraph.Edge[] Edges { get; } = new AgvGraph.Edge[]
        {
            new("node00", "node01"),
            new("node01", "node11"),
            new("node11", "node10"),
            new("node10", "node00")
        };

        private Mock<IAgvRegistry> GetAgvRegistry()
        {
            var registry = new Mock<IAgvRegistry>();

            var agvs = new List<IAgv>();

            for (int i = 0; i < AgvIds.Length; i++)
            {
                string agvId = AgvIds[i];
                string order = Orders[i];
                string state = States[i];
                string ip = Ips[i];
                var correlationId = agvId.ToGuid();

                const string mapName = "Map#1";
                var agvGraph = new AgvGraph(correlationId, Nodes, Edges, mapName.ToGuid(), mapName);
                var agvStatus = agvId == "AGV003" ? null : new AgvStatus(correlationId, order, true, true, 10, true, true);
                var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());

                agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph, agvData));
            }

            registry.SetupGet(x => x.Count).Returns(agvs.Count);
            registry.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
            registry.Setup(x => x.GetReadyAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
            registry.Setup(x => x.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(agvs));
            registry.Setup(x => x.GetAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId =>
            {
                foreach (var agv in agvs)
                {
                    if (agv.CorrelationId == correlationId)
                    {
                        return agv;
                    }
                }
                return null;
            });

            return registry;
        }

        private Mock<IAgvPositionRegistry> GetAgvPositionRegistry()
        {
            var registry = new Mock<IAgvPositionRegistry>();
            var agvPositions = new List<AgvPositionStatus>();

            for (int i = 0; i < AgvIds.Length; i++)
            {
                string agvId = AgvIds[i];
                var correlationId = agvId.ToGuid();
                var node = Nodes[i];

                agvPositions.Add(new AgvPositionStatus(correlationId, agvId, new AgvPosition(true, node.X, node.Y, node.Z, 0.5f)));
            }

            registry.Setup(x => x.GetAllPositions()).Returns(agvPositions);
            registry.Setup(x => x.GetPosition(It.IsAny<Guid>())).Returns<Guid>(correlationId =>
            {
                foreach (var agvPosition in agvPositions)
                {
                    if (agvPosition.CorrelationId == correlationId)
                    {
                        return agvPosition;
                    }
                }
                return null;
            });

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
            ServiceActions.Add(serviceAction);
            return this;
        }

        /// <inheritdoc/>
        protected override AgvController GetController()
        {
            IRequestClient<GetAgvsRequest> getAgvsRequestClient = GetRequestClient<GetAgvsRequest>();
            IRequestClient<GetAgvGraphRequest> getAgvGraphRequestClient = GetRequestClient<GetAgvGraphRequest>();
            IRequestClient<GetAgvStatusRequest> getAgvStatusRequestClient = GetRequestClient<GetAgvStatusRequest>();
            IRequestClient<GetAgvDataRequest> getAgvDataRequestClient = GetRequestClient<GetAgvDataRequest>();
            IRequestClient<GetAgvPositionsRequest> getAgvPositionsRequestClient = GetRequestClient<GetAgvPositionsRequest>();
            IRequestClient<SetIdleCommand> setIdleRequestClient = GetRequestClient<SetIdleCommand>();
            IRequestClient<LockAgvCommand> lockAgvRequestClient = GetRequestClient<LockAgvCommand>();
            IRequestClient<RequestAgvDataCommand> emitRequestDataRequestClient = GetRequestClient<RequestAgvDataCommand>();
            IRequestClient<RequestAgvGraphCommand> emitRequestGraphRequestClient = GetRequestClient<RequestAgvGraphCommand>();

            var controller = new AgvController(getAgvsRequestClient, getAgvGraphRequestClient, getAgvStatusRequestClient, getAgvDataRequestClient,
                getAgvPositionsRequestClient, setIdleRequestClient, lockAgvRequestClient, emitRequestDataRequestClient, emitRequestGraphRequestClient)
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
        public AgvControllerTestFixture<TRequest, TRequestHandler> UseDefaultServices()
        {
            ServiceActions.Add(sc => sc.AddTransient(_ => GetAgvRegistry().Object));
            ServiceActions.Add(sc => sc.AddTransient(_ => GetAgvPositionRegistry().Object));
            ServiceActions.Add(sc => sc.AddTransient(_ => GetMapper()));
            ServiceActions.Add(sc => sc.AddTransient(_ => new Mock<IBus>().Object));
            ServiceActions.Add(sc => sc.AddTransient(_ => new Mock<IAgvPositionRegistry>().Object));
            return this;
        }

        /// <summary>
        /// Adds the <see cref="IAgvRegistry"/> mock object
        /// </summary>
        /// <returns></returns>
        [UsedImplicitly]
        public AgvControllerTestFixture<TRequest, TRequestHandler> AddAgvRegistryMock(Action<Mock<IAgvRegistry>> agvRegistryAction)
        {
            var agvRegistryMock = new Mock<IAgvRegistry>();
            agvRegistryAction.Invoke(agvRegistryMock);

            ServiceActions.Add(sc => sc.AddTransient(_ => agvRegistryMock.Object));
            return this;
        }

        public new AgvControllerTestFixture<TRequest, TRequestHandler> Build()
        {
            base.Build();
            return this;
        }
    }
}