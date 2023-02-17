using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Maps.Handlers.Queries
{
    /// <summary>
    /// Testing fixture to test map request
    /// </summary>
    /// <typeparam name="TRequestClient"></typeparam>
    /// <typeparam name="TConsumer"></typeparam>
    public class MapRequestTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer> 
        where TRequestClient : class
        where TConsumer : class, IConsumer
    {
        public const string Base64String = "dGhlIHF1aWNrIGJyb3duIGZveCBqdW1wIG92ZXIgdGhlIGxhenkgZG9n";

        private readonly string[] _agvIds = { "AGV001", "AGV002", "AGV003" };
        private readonly string[] _orders = { "ORDER001", "ORDER002", "" };
        private readonly string[] _states = { nameof(AgvStateMachine.Ready), nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Discovered) };
        private readonly string[] _ips = { "127.0.0.1", "127.0.0.2", "127.0.0.3" };

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

        private readonly List<Action<IServiceCollection>> _serviceActions = new();

        protected override void ConfigureServices(IServiceCollection collection)
        {
            foreach (Action<IServiceCollection> serviceAction in _serviceActions)
            {
                serviceAction.Invoke(collection);
            }
        }

        /// <summary>
        /// Adds the service to the service collection
        /// </summary>
        /// <param name="serviceAction">The action of the service to add</param>
        /// <returns></returns>
        [UsedImplicitly]
        public MapRequestTestFixture<TRequestClient, TConsumer> AddService(Action<IServiceCollection> serviceAction)
        {
            _serviceActions.Add(serviceAction);
            return this;
        }

        /// <summary>
        /// Uses the default services
        /// </summary>
        /// <returns></returns>
        public MapRequestTestFixture<TRequestClient, TConsumer> UseDefaultServices()
        {
            _serviceActions.Add(sc => sc.AddTransient(_ => GetMapRegistry().Object));
            _serviceActions.Add(sc => sc.AddTransient(_ => GetAgvRegistry().Object));
            _serviceActions.Add(sc => sc.AddTransient(_ => GetMapper()));

            return this;
        }

        /// <summary>
        /// Uses the default services
        /// </summary>
        /// <returns></returns>
        public MapRequestTestFixture<TRequestClient, TConsumer> AddMapRegistryMock(Action<Mock<IMapRegistry>> mapRegistryAction)
        {
            var mapRegistryMock = new Mock<IMapRegistry>();
            mapRegistryAction.Invoke(mapRegistryMock);

            _serviceActions.Add(sc => sc.AddTransient(_ => mapRegistryMock.Object));

            return this;
        }

        public new MapRequestTestFixture<TRequestClient, TConsumer> Build()
        {
            base.Build();
            return this;
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
                string ip = _ips[i];
                var correlationId = _correlationIds[i];

                const string mapName = "Map#1";
                var agvGraph = new AgvGraph(correlationId, _nodes, _edges, _correlationIds[2], mapName);
                var agvStatus = new AgvStatus(correlationId, order, true, true, 10, true, true);
                var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());

                agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph, agvData));
            }

            registryMock.SetupGet(x => x.Count).Returns(agvs.Count);
            registryMock.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Ready)).Count);
            registryMock.Setup(x => x.GetReadyAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Ready))));
            registryMock.Setup(x => x.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(agvs));
            registryMock.Setup(x => x.GetAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId => agvs.SingleOrDefault(a => a.CorrelationId == correlationId));

            return registryMock;
        }

        private IMapper GetMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });
            var mapper = new Mapper(mapperConfig);
            return mapper;
        }

    }
}
