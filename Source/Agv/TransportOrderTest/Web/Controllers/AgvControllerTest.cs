using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Web.Controllers
{
    /// <summary>
    /// Tests TransportOrderApiController
    /// </summary>
    [TestClass]
    public class AgvControllerTest
    {
        #region Setup

        private static readonly string[] _agvIds = { "AGV001", "AGV002" };
        private static readonly string[] _orders = { "ORDER001", "ORDER002" };
        private static readonly string[] _states = { "Idle", "Busy" };
        private static readonly string[] _ips = { "127.0.0.1", "127.0.0.2" };

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

        private IAgvRegistry GetAgvRegistry()
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

                var agvGraph = new AgvGraph(correlationId, _nodes, _edges);
                var agvStatus = new AgvStatus(correlationId, order, true, true, 10, true, true);
                var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());

                agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph, agvData));
            }

            registry.SetupGet(x => x.Count).Returns(agvs.Count);
            registry.SetupGet(x => x.CountIdle).Returns(agvs.Count);
            registry.Setup(x => x.GetIdleAgvs()).Returns(agvs);
            registry.Setup(x => x.GetAllAgvs()).Returns(agvs);
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

        #endregion

        /// <summary/>
        [TestMethod]
        public async Task GetAgvs_Successfully()
        {
            var controller = new AgvController(GetAgvRegistry());

            var result = await controller.GetAgvs() as OkObjectResult;
            Assert.IsNotNull(result);

            var agvs = result.Value as List<TransportOrder.Web.Models.Agv>;
            Assert.IsNotNull(agvs);
            Assert.AreEqual(_agvIds.Length, agvs.Count);

            for (int index = 0; index < _agvIds.Length; index++)
            {
                string agvId = _agvIds[index];
                string state = _states[index];

                Assert.AreEqual(agvId.ToGuid(), agvs[index].CorrelationId);
                Assert.AreEqual(agvId, agvs[index].AgvId);
                Assert.AreEqual(state, agvs[index].CurrentState);
            }
        }

        /// <summary/>
        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void GetAgvGraph_Successfully(int index)
        {
            var controller = new AgvController(GetAgvRegistry());
            var correlationId = _agvIds[index].ToGuid();

            var result = controller.GetAgvGraph(correlationId) as OkObjectResult;
            Assert.IsNotNull(result);

            var agvGraph = result.Value as AgvGraph;
            Assert.IsNotNull(agvGraph);

            Assert.AreEqual(correlationId, agvGraph.CorrelationId);
            Assert.AreEqual(_nodes[index].Id, agvGraph.Nodes[index].Id);
            Assert.AreEqual(_edges[index].From, agvGraph.Edges[index].From);
        }

        /// <summary/>
        [TestMethod]
        public void GetAgvGraph_NotFound()
        {
            var controller = new AgvController(GetAgvRegistry());
            var correlationId = Guid.Empty;

            var result = controller.GetAgvGraph(correlationId) as NotFoundResult;
            Assert.IsNotNull(result);
        }

        /// <summary/>
        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void GetAgvStatus_Successfully(int index)
        {
            var controller = new AgvController(GetAgvRegistry());
            var correlationId = _agvIds[index].ToGuid();

            var result = controller.GetAgvStatus(correlationId) as OkObjectResult;
            Assert.IsNotNull(result);

            var agvStatus = result.Value as AgvStatus;
            Assert.IsNotNull(agvStatus);

            Assert.AreEqual(correlationId, agvStatus.CorrelationId);
            Assert.AreEqual(_orders[index], agvStatus.OrderId);
        }

        /// <summary/>
        [TestMethod]
        public void GetAgvStatus_NotFound()
        {
            var controller = new AgvController(GetAgvRegistry());
            var correlationId = Guid.Empty;

            var result = controller.GetAgvStatus(correlationId) as NotFoundResult;
            Assert.IsNotNull(result);
        }

        /// <summary/>
        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void GetAgvData_Successfully(int index)
        {
            var controller = new AgvController(GetAgvRegistry());
            var correlationId = _agvIds[index].ToGuid();

            var result = controller.GetAgvData(correlationId) as OkObjectResult;
            Assert.IsNotNull(result);

            var agvData = result.Value as AgvData;
            Assert.IsNotNull(agvData);

            Assert.AreEqual(correlationId, agvData.CorrelationId);
            Assert.AreEqual(_ips[index], agvData.IpV4Address);
        }

        /// <summary/>
        [TestMethod]
        public void GetAgvData_NotFound()
        {
            var controller = new AgvController(GetAgvRegistry());
            var correlationId = Guid.Empty;

            var result = controller.GetAgvData(correlationId) as NotFoundResult;
            Assert.IsNotNull(result);
        }
    }
}
