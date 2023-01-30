using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using MassTransit;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    [TestClass]
    public class AgvControllerTest
    {
        #region Setup
        private static readonly string[] _agvIds = { "AGV001", "AGV002", "AGV003" };
        private static readonly string[] _orders = { "ORDER001", "ORDER002", "" };
        private static readonly string[] _states = { nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Discovered) };
        private static readonly string[] _ips = { "127.0.0.1", "127.0.0.2", "127.0.0.3" };

        private static readonly AgvGraph.Node[] _nodes = new AgvGraph.Node[]
        {
            new("node00") { X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node10") { X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node01") { X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
            new("node11") { X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint }
        };

        private static readonly AgvGraph.Edge[] _edges = new AgvGraph.Edge[]
        {
            new("node00", "node01"),
            new("node01", "node11"),
            new("node11", "node10"),
            new("node10", "node00")
        };

        private Mapper GetMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile(new MappingProfile()); });

            var mapper = new Mapper(mapperConfig);
            return mapper;
        }

        private static IAgvRegistry GetAgvRegistry()
        {
            var registry = new Mock<IAgvRegistry>();

            var agvs = new List<IAgv>();

            for (int i = 0; i < _agvIds.Length; i++)
            {
                string agvId = _agvIds[i];
                string order = _orders[i];
                string state = _states[i];
                string ip = _ips[i];
                var correlationId = agvId.ToGuid();

                const string mapName = "Map#1";
                var agvGraph = new AgvGraph(correlationId, _nodes, _edges, mapName.ToGuid(), mapName);
                var agvStatus = agvId == "AGV003" ? null : new AgvStatus(correlationId, order, true, true, 10, true, true);
                var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());

                agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph, agvData));
            }

            registry.SetupGet(x => x.Count).Returns(agvs.Count);
            registry.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
            registry.Setup(x => x.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
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

            return registry.Object;
        }

        private static IAgvPositionRegistry GetAgvPositionRegistry()
        {
            var registry = new Mock<IAgvPositionRegistry>();
            var agvPositions = new List<AgvPositionStatus>();

            for (int i = 0; i < _agvIds.Length; i++)
            {
                string agvId = _agvIds[i];
                var correlationId = agvId.ToGuid();
                var node = _nodes[i];

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

            return registry.Object;
        }

        private AgvController GetController()
        {
            var bus = new Mock<IBus>();
            return new AgvController(
                GetAgvRegistry(), GetAgvPositionRegistry(), bus.Object, GetMapper());
        }
        #endregion

        [TestMethod]
        public void GetAgvs_Successfully()
        {
            var controller = GetController();

            var result = controller.GetAgvs().Result as OkObjectResult;
            result.ShouldNotBeNull();

            var agvsIterable = result.Value as IEnumerable<AgvDto>;
            // ReSharper disable once PossibleMultipleEnumeration
            agvsIterable.ShouldNotBeNull();
            // ReSharper disable once PossibleMultipleEnumeration
            List<AgvDto> agvs = agvsIterable.ToList();
            agvs.Count.ShouldBe(_agvIds.Length);

            for (int index = 0; index < _agvIds.Length; index++)
            {
                string agvId = _agvIds[index];
                string state = _states[index];

                agvs[index].CorrelationId.ShouldBe(agvId.ToGuid());
                agvs[index].AgvId.ShouldBe(agvId);
                agvs[index].CurrentState.ShouldBe(state);
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void GetAgvGraph_Successfully(int index)
        {
            var controller = GetController();
            var correlationId = _agvIds[index].ToGuid();

            var result = controller.GetAgvGraph(correlationId).Result as OkObjectResult;
            result.ShouldNotBeNull();

            var graph = result.Value as GraphDto;
            graph.ShouldNotBeNull();

            graph.CorrelationId.ShouldBe(correlationId);
            graph.Nodes[index].Id.ShouldBe(_nodes[index].Id);
            graph.Edges[index].From.Id.ShouldBe(_edges[index].From);
            graph.Edges[index].To.Id.ShouldBe(_edges[index].To);
        }

        [TestMethod]
        public void GetAgvGraph_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.GetAgvGraph(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void GetAgvStatus_Successfully(int index)
        {
            var controller = GetController();
            var correlationId = _agvIds[index].ToGuid();

            var result = controller.GetAgvStatus(correlationId).Result as OkObjectResult;
            result.ShouldNotBeNull();

            var agvStatus = result.Value as AgvStateDto;
            agvStatus.ShouldNotBeNull();

            agvStatus.CorrelationId.ShouldBe(correlationId);
            agvStatus.OrderId.ShouldBe(_orders[index]);
        }

        [TestMethod]
        public void GetAgvStatus_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.GetAgvStatus(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void GetAgvStatus_NoContent()
        {
            var controller = GetController();
            var correlationId = _agvIds[2].ToGuid(); // AGV without AgvStatus

            var result = controller.GetAgvStatus(correlationId).Result as NoContentResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void GetAgvData_Successfully(int index)
        {
            var controller = GetController();
            var correlationId = _agvIds[index].ToGuid();

            var result = controller.GetAgvData(correlationId).Result as OkObjectResult;
            result.ShouldNotBeNull();

            var agvData = result.Value as AgvData;
            agvData.ShouldNotBeNull();

            agvData.CorrelationId.ShouldBe(correlationId);
            agvData.IpV4Address.ShouldBe(_ips[index]);
        }

        [TestMethod]
        public void GetAgvData_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.GetAgvData(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void SetIdle_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.SetIdle(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void SetIdle_BadRequest()
        {
            var controller = GetController();
            var correlationId = _agvIds[1].ToGuid(); // AGV not in state 'Discovered'

            var result = controller.SetIdle(correlationId).Result as BadRequestObjectResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void SetIdle_Ok()
        {
            var bus = new Mock<IBus>();

            var controller = new AgvController(GetAgvRegistry(), GetAgvPositionRegistry(), bus.Object, GetMapper());
            var correlationId = _agvIds[2].ToGuid();

            var result = controller.SetIdle(correlationId).Result as OkResult;
            result.ShouldNotBeNull();

            bus.Verify(s => s.Publish(It.IsAny<ForceReleaseAgv>(), default), Times.Once);
        }

        [TestMethod]
        public void LockAgv_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.LockAgv(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void LockAgv_Ok()
        {
            var bus = new Mock<IBus>();

            var controller = new AgvController(GetAgvRegistry(), GetAgvPositionRegistry(), bus.Object, GetMapper());
            var correlationId = _agvIds[2].ToGuid();

            var result = controller.LockAgv(correlationId).Result as OkResult;
            result.ShouldNotBeNull();

            bus.Verify(s => s.Publish(It.IsAny<LockAgv>(), default), Times.Once);
        }

        [TestMethod]
        public void EmitRequestAgvData_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.EmitRequestData(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void EmitRequestAgvData_Ok()
        {
            var bus = new Mock<IBus>();

            var controller = new AgvController(GetAgvRegistry(), GetAgvPositionRegistry(), bus.Object, GetMapper());
            var correlationId = _agvIds[2].ToGuid();

            var result = controller.EmitRequestData(correlationId).Result as OkResult;
            result.ShouldNotBeNull();

            bus.Verify(s => s.Publish(It.IsAny<RequestAgvData>(), default), Times.Once);
        }

        [TestMethod]
        public void EmitRequestAgvGraph_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.EmitRequestGraph(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void EmitRequestAgvGraph_Ok()
        {
            var bus = new Mock<IBus>();

            var controller = new AgvController(GetAgvRegistry(), GetAgvPositionRegistry(), bus.Object, GetMapper());
            var correlationId = _agvIds[2].ToGuid();

            var result = controller.EmitRequestGraph(correlationId).Result as OkResult;
            result.ShouldNotBeNull();

            bus.Verify(s => s.Publish(It.IsAny<RequestAgvGraph>(), default), Times.Once);
        }
    }
}