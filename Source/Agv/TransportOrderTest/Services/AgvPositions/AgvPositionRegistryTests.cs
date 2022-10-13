using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services.AgvPositions
{
    /// <summary>
    /// Tests for AgvPositionRegistry
    /// </summary>
    [TestClass]
    public class AgvPositionRegistryTests
    {
        /// <summary/>
        [TestMethod]
        public void UpdatePosition_AddsObject_WhenRegistryIsEmpty()
        {
            var fixture = new AgvPositionRegistryTestFixture();
            var agvPositionRegistry = fixture.AgvPositionRegistry;
            var agvPositionReceived = fixture.AgvPositionReceived;

            List<AgvPositionReceived> positions = agvPositionRegistry.GetAllPositions();

            positions.ShouldNotBeNull();
            positions.Count.ShouldBe(0);
            agvPositionRegistry.UpdatePosition(agvPositionReceived);

            positions = agvPositionRegistry.GetAllPositions();
            positions.Count.ShouldBe(1);
            positions.First().ShouldBe(agvPositionReceived);
        }

        /// <summary/>
        [TestMethod]
        public void UpdatePosition_StoresNewPosition_WhenRegistryContainsSingleAgv()
        {
            var fixture = new AgvPositionRegistryTestFixture();
            var newAgvPosition = new Common.Messages.AgvManagement.AgvPosition(true, 2, 3, 4, 0);
            var newAgvPositionReceived = new AgvPositionReceived(fixture.CorrelationId, fixture.AgvId, newAgvPosition);
            var agvPositionReceived = fixture.AgvPositionReceived;
            var agvPositionRegistry = fixture.AgvPositionRegistry;

            agvPositionRegistry.UpdatePosition(agvPositionReceived);

            List<AgvPositionReceived> positions = agvPositionRegistry.GetAllPositions();

            positions.Count.ShouldBe(1);
            positions.First().ShouldBe(agvPositionReceived);

            agvPositionRegistry.UpdatePosition(newAgvPositionReceived);

            positions = agvPositionRegistry.GetAllPositions();
            positions.Count.ShouldBe(1);
            positions.First().ShouldBe(newAgvPositionReceived);
        }

        /// <summary/>
        [TestMethod]
        public void UpdatePosition_PublishesAgvPositionChangedOnTheBus_WhenTheChangeDetectorReturnsTrue()
        {
            var fixture = new AgvPositionRegistryTestFixture();
            fixture.ChangeDetectorMock.Setup(c => c.HasChanged(It.IsAny<AgvPositionReceived>(), It.IsAny<AgvPositionReceived>()))
                .Returns(true);

            var agvPositionReceived = fixture.AgvPositionReceived;
            fixture.AgvPositionRegistry.UpdatePosition(agvPositionReceived);

            fixture.BusMock.Verify(b => b.Publish(It.Is<AgvPositionChanged>(o =>
                    o.NewAgvPosition == agvPositionReceived.AgvPosition &&
                    o.AgvId == agvPositionReceived.AgvId),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        /// <summary/>
        [TestMethod]
        public void UpdatePosition_DoesNotPublishAgvPositionChangedOnTheBus_WhenTheChangeDetectorReturnsFalse()
        {
            var fixture = new AgvPositionRegistryTestFixture();
            fixture.ChangeDetectorMock.Setup(c => c.HasChanged(It.IsAny<AgvPositionReceived>(), It.IsAny<AgvPositionReceived>()))
                .Returns(false);

            fixture.AgvPositionRegistry.UpdatePosition(fixture.AgvPositionReceived);

            fixture.BusMock.Verify(b => b.Publish(It.IsAny<AgvPositionChanged>(), default), Times.Never);
        }

        /// <summary/>
        [TestMethod]
        public void GetPosition_ReturnsNull_WhenRegistryIsEmpty()
        {
            var fixture = new AgvPositionRegistryTestFixture();

            var retrievedPosition = fixture.AgvPositionRegistry.GetPosition(fixture.CorrelationId);
            retrievedPosition.ShouldBeNull();
        }
    }
}
