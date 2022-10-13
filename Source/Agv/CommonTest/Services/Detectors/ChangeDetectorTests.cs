using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services.Detectors
{
    /// <summary>
    /// </summary>
    [TestClass]
    public class ChangeDetectorTests
    {
        private static AgvPositionReceived GetAgvPosition(string agvId) => new(Guid.NewGuid(), agvId, new AgvPosition(true, 0, 0, 0, 0));

        /// <summary>
        /// </summary>
        [TestMethod]
        public void HasChanged_ReturnsTrue_WhenPreviousObjectIsNull()
        {
            var changeDetector = new ChangeDetector<AgvPositionReceived>();
            changeDetector.HasChanged(null, GetAgvPosition("agv")).ShouldBeTrue();
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void HasChanged_ReturnsTrue_WhenNewObjectIsNull()
        {
            var changeDetector = new ChangeDetector<AgvPositionReceived>();
            changeDetector.HasChanged(GetAgvPosition("agv"), null).ShouldBeTrue();
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void HasChanged_ReturnsFalse_WhenBothObjectsAreNull()
        {
            var changeDetector = new ChangeDetector<AgvPositionReceived>();
            changeDetector.HasChanged(null, null).ShouldBeFalse();
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void HasChanged_ReturnsTrue_WhenObjectAreNotEqual()
        {
            var changeDetector = new ChangeDetector<AgvPositionReceived>();
            changeDetector.HasChanged(GetAgvPosition("agv1"), GetAgvPosition("agv2")).ShouldBeTrue();
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void HasChanged_ReturnsFalse_WhenObjectAreEqual()
        {
            var changeDetector = new ChangeDetector<AgvPositionReceived>();
            var agvPositionReceived = GetAgvPosition("agv1");
            changeDetector.HasChanged(agvPositionReceived, agvPositionReceived).ShouldBeFalse();
        }

    }
}
