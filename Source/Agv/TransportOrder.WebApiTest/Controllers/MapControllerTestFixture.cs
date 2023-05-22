using System.Text;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    public class MapControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, MapController>
        where TRequest : class
        where TRequestHandler : class, IConsumer
    {
        public const string Base64String = "dGhlIHF1aWNrIGJyb3duIGZveCBqdW1wIG92ZXIgdGhlIGxhenkgZG9n";

        private readonly string[] _agvIds = { "AGV001", "AGV002", "AGV003" };
        private readonly string[] _orders = { "ORDER001", "ORDER002", "" };
        private readonly string[] _states = { nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Discovered) };

        private readonly AgvGraph.Node[] _nodes = {
            new("node00") { X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node10") { X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node01") { X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node11") { X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint }
        };

        private readonly AgvGraph.Edge[] _edges = {
            new("node00", "node01"),
            new("node01", "node11"),
            new("node11", "node10"),
            new("node10", "node00")
        };

        private readonly Guid[] _correlationIds = {
            new("7FE9372B-4219-4D09-8885-966EE125A350"),
            new("041AD79E-1952-4AB7-BD3E-9EC972E27FBF"),
            new("465A85A6-6A02-4068-A7AA-E3B4B9821019")
        };

        protected override MapController GetController()
        {
            IRequestClient<GetMapListRequest> getMapListRequestClient = GetRequestClient<GetMapListRequest>();
            IRequestClient<GetMapImageRequest> getMapImageRequestClient = GetRequestClient<GetMapImageRequest>();

            var controller = new MapController(getMapListRequestClient, getMapImageRequestClient);

            return controller;
        }

        private Mock<IMapRegistry> GetMapRegistry()
        {
            var mapRegistryMock = new Mock<IMapRegistry>();

            mapRegistryMock.Setup(x => x.GetAllMaps()).Returns(new EnumerableQuery<Map>(new List<Map>
            {
                new(_correlationIds[0], "Area51_Floor1_Compartment47", Encoding.UTF8.GetBytes(Base64String), 0.01, 1, 2, DateTime.UtcNow),
                new(_correlationIds[1], "Area51_Floor1_Compartment48", Encoding.UTF8.GetBytes(Base64String), 0.02, 2, 3, DateTime.UtcNow),
                new(_correlationIds[2], "Area51_Floor1_Compartment49", Encoding.UTF8.GetBytes(Base64String), 0.03, 2, 1, DateTime.UtcNow)
            }));
            mapRegistryMock.Setup(x => x.GetMap(It.IsAny<Guid>())).Returns(Encoding.UTF8.GetBytes(Base64String));

            return mapRegistryMock;
        }

        private Mock<IAgvRegistry> GetAgvRegistry()
        {
            var registryMock = new Mock<IAgvRegistry>();

            var agvs = new List<IAgv>();

            for (int i = 0; i < _agvIds.Length; i++)
            {
                string agvId = _agvIds[i];
                string order = _orders[i];
                string state = _states[i];
                var correlationId = _correlationIds[i];

                const string mapName = "Map#1";
                var agvGraph = new AgvGraph(correlationId, _nodes, _edges, _correlationIds[2], mapName);
                var agvStatus = new AgvStatus(correlationId, true, true, 10, true, true);

                agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph));
            }

            registryMock.SetupGet(x => x.Count).Returns(agvs.Count);
            registryMock.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
            registryMock.Setup(x => x.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
            registryMock.Setup(x => x.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(agvs));
            registryMock.Setup(x => x.GetAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId => agvs.SingleOrDefault(a => a.CorrelationId == correlationId));

            return registryMock;
        }

        /// <summary>
        /// Adds the service to the service collection
        /// </summary>
        /// <param name="serviceAction">The action of the service to add</param>
        /// <returns></returns>
        [UsedImplicitly]
        public MapControllerTestFixture<TRequest, TRequestHandler> AddService(Action<IServiceCollection> serviceAction)
        {
            AddServices(serviceAction);
            return this;
        }

        /// <summary>
        /// Uses the default services
        /// </summary>
        /// <returns></returns>
        public MapControllerTestFixture<TRequest, TRequestHandler> UseDefaultServices()
        {
            AddServices(sc => sc.AddTransient(_ => GetMapRegistry().Object));
            AddServices(sc => sc.AddTransient(_ => GetAgvRegistry().Object));
            AddServices(sc => sc.AddTransient(_ => GetMapper()));

            return this;
        }

        /// <summary>
        /// Adds the <see cref="IMapRegistry"/> mock object
        /// </summary>
        /// <returns></returns>
        [UsedImplicitly]
        public MapControllerTestFixture<TRequest, TRequestHandler> AddMapRegistryMock(Action<Mock<IMapRegistry>> mapRegistryAction)
        {
            var mapRegistryMock = new Mock<IMapRegistry>();
            mapRegistryAction.Invoke(mapRegistryMock);

            AddServices(sc => sc.AddTransient(_ => mapRegistryMock.Object));

            return this;
        }

        /// <summary>
        /// Adds the <see cref="IMapRegistry"/> mock object
        /// </summary>
        /// <returns></returns>
        public MapControllerTestFixture<TRequest, TRequestHandler> AddMapRegistryMock()
        {
            AddServices(sc => sc.AddTransient(_ => GetMapRegistry().Object));

            return this;
        }

        public new MapControllerTestFixture<TRequest, TRequestHandler> Build()
        {
            base.Build();
            return this;
        }
    }
}
