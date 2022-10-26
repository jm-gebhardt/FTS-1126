using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    /// <summary>
    /// Tests TransportOrderApiController
    /// </summary>
    [TestClass]
    public class MapControllerTest
    {
        #region Setup

        private static readonly string[] _agvIds = { "AGV001", "AGV002", "AGV003" };
        private static readonly string[] _orders = { "ORDER001", "ORDER002", "" };
        private static readonly string[] _states = { nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Discovered) };
        private static readonly string[] _ips = { "127.0.0.1", "127.0.0.2", "127.0.0.3" };

        private static readonly AgvGraph.Node[] _nodes = new AgvGraph.Node[] {
            new("node00") { X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node10") { X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node01") { X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node11") { X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint }
        };

        private static readonly AgvGraph.Edge[] _edges = new AgvGraph.Edge[] {
            new("node00", "node01"),
            new("node01", "node11"),
            new("node11", "node10"),
            new("node10", "node00")
        };

        private static readonly Guid[] _correlationIds = new Guid[]
        {
            new("7FE9372B-4219-4D09-8885-966EE125A350"),
            new("041AD79E-1952-4AB7-BD3E-9EC972E27FBF"),
            new("465A85A6-6A02-4068-A7AA-E3B4B9821019")
        };

        private readonly Mock<IAgvRegistry> _registryMock;
        private readonly Mock<IMapRegistry> _mapRegistryMock;

        /// <summary>
        /// Ctor
        /// </summary>
        public MapControllerTest()
        {
            _registryMock = GetAgvRegistry();
            _mapRegistryMock = GetMapRegistry();
        }

        private Mapper GetMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });
            var mapper = new Mapper(mapperConfig);
            return mapper;
        }

        private static Mock<IAgvRegistry> GetAgvRegistry()
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
            registryMock.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
            registryMock.Setup(x => x.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
            registryMock.Setup(x => x.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(agvs));
            registryMock.Setup(x => x.GetAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId => agvs.SingleOrDefault(a => a.CorrelationId == correlationId));

            return registryMock;
        }

        private static Mock<IMapRegistry> GetMapRegistry()
        {
            string base64String = "dGhlIHF1aWNrIGJyb3duIGZveCBqdW1wIG92ZXIgdGhlIGxhenkgZG9n";

            var mapRegistryMock = new Mock<IMapRegistry>();

            mapRegistryMock.Setup(x => x.GetAllMaps()).Returns(new EnumerableQuery<Persistence.Models.Map>(new List<Persistence.Models.Map>
            {
                new(_correlationIds[0], "Area51_Floor1_Compartment47", Encoding.UTF8.GetBytes(base64String), 0.01, 1, 2, DateTime.Now),
                new(_correlationIds[1], "Area51_Floor1_Compartment48", Encoding.UTF8.GetBytes(base64String), 0.02, 2, 3, DateTime.Now),
                new(_correlationIds[2], "Area51_Floor1_Compartment49", Encoding.UTF8.GetBytes(base64String), 0.03, 2, 1, DateTime.Now)
            }));

            return mapRegistryMock;
        }

        #endregion

        /// <summary/>
        [TestMethod]
        public void GetMaps_Successfully()
        {
            var controller = new MapController(_registryMock.Object, _mapRegistryMock.Object, GetMapper());

            var result = controller.GetMaps().Result as OkObjectResult;

            List<MapDto>? dtoMaps = (result!.Value as IEnumerable<MapDto>)?.ToList();

            dtoMaps.ShouldNotBeEmpty();
            dtoMaps.ShouldAllBe(p => _correlationIds.Any(id => id == p.CorrelationId));
        }
    }
}
