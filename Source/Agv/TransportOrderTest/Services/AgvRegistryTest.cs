using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Tests for AgvRegistry
    /// </summary>
    [TestClass]
    public class AgvRegistryTest
    {
        /// <summary>
        /// Add AGVs to the registry
        /// </summary>
        [TestMethod]
        public void AddAgv()
        {
            var agv1 = new Common.Messages.AgvManagement.Agv("AGV001", AgvState.Idle);
            var agv2 = new Common.Messages.AgvManagement.Agv("AGV002", AgvState.Idle);

            var agvRegistry = new InMemoryAgvRegistry();
            Assert.AreEqual(0, agvRegistry.Count);

            agvRegistry.AddOrUpdate(agv1.AgvId.ToGuid(), agv1);
            Assert.AreEqual(1, agvRegistry.Count);

            agvRegistry.AddOrUpdate(agv2.AgvId.ToGuid(), agv2);
            Assert.AreEqual(2, agvRegistry.Count);
        }

        /// <summary>
        /// Find AGV in the Idle
        /// </summary>
        [TestMethod]
        public void TryToReserveIdleAgv()
        {
            var agv1 = new Common.Messages.AgvManagement.Agv("AGV001", AgvState.Busy);
            var agv2 = new Common.Messages.AgvManagement.Agv("AGV001", AgvState.Idle);
            var agvRegistry = new InMemoryAgvRegistry();

            agvRegistry.AddOrUpdate(agv1.AgvId.ToGuid(), agv1);
            agvRegistry.AddOrUpdate(agv2.AgvId.ToGuid(), agv2);

            var agv = agvRegistry.ReserveIdleAgv();
            Assert.IsNotNull(agv);
            Assert.AreEqual(agv2.AgvId, agv.AgvId); // Must be second AGV because first AGV is not Idle
        }

        /// <summary>
        /// Try to find an AGV with an IDle state when the registry is empty
        /// </summary>
        [TestMethod]
        public void TryToReserveIdleAgvWhenAllAgvsAreBusy()
        {
            var agv1 = new Common.Messages.AgvManagement.Agv("AGV001", AgvState.Busy);
            var agv2 = new Common.Messages.AgvManagement.Agv("AGV002", AgvState.Busy);
            var agvRegistry = new InMemoryAgvRegistry();

            agvRegistry.AddOrUpdate(agv1.AgvId.ToGuid(), agv1);
            agvRegistry.AddOrUpdate(agv2.AgvId.ToGuid(), agv2);

            var agv = agvRegistry.ReserveIdleAgv();
            Assert.IsNull(agv);
        }

        /// <summary>
        /// Try to find an AGV with an IDle state when the registry is empty
        /// </summary>
        [TestMethod]
        public void TryToReserveIdleAgvFromEmptyRegistry()
        {
            var agvRegistry = new InMemoryAgvRegistry();

            var agv = agvRegistry.ReserveIdleAgv();
            Assert.IsNull(agv);
        }
    }
}