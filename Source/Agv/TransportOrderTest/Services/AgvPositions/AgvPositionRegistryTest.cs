using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services.AgvPositions
{
    [TestClass]
    public class AgvPositionRegistryTest
    {
        [TestMethod]
        public void UpdatePosition_AddsObject_WhenRegistryIsEmpty()
        {
            var fixture = new AgvPositionRegistryTestFixture();
            var agvPositionRegistry = fixture.AgvPositionRegistry;
            var agvPositionStatus = fixture.AgvPositionStatus;

            List<AgvPositionStatus> positions = agvPositionRegistry.GetAllPositions();

            positions.ShouldNotBeNull();
            positions.Count.ShouldBe(0);
            agvPositionRegistry.UpdatePosition(agvPositionStatus);

            positions = agvPositionRegistry.GetAllPositions();
            positions.Count.ShouldBe(1);
            positions.First().ShouldBe(agvPositionStatus);
        }

        [TestMethod]
        public void UpdatePosition_StoresNewPosition_WhenRegistryContainsSingleAgv()
        {
            var fixture = new AgvPositionRegistryTestFixture();
            var newAgvPosition = new Common.Messages.AgvManagement.AgvPosition(true, 2, 3, 4, 0);
            var newAgvPositionUpdated = new AgvPositionStatus(fixture.CorrelationId, fixture.AgvId, newAgvPosition);
            var agvPositionStatus = fixture.AgvPositionStatus;
            var agvPositionRegistry = fixture.AgvPositionRegistry;

            agvPositionRegistry.UpdatePosition(agvPositionStatus);

            List<AgvPositionStatus> positions = agvPositionRegistry.GetAllPositions();

            positions.Count.ShouldBe(1);
            positions.First().ShouldBe(agvPositionStatus);

            agvPositionRegistry.UpdatePosition(newAgvPositionUpdated);

            positions = agvPositionRegistry.GetAllPositions();
            positions.Count.ShouldBe(1);
            positions.First().ShouldBe(newAgvPositionUpdated);
        }

        [TestMethod]
        public void UpdatePosition_PublishesAgvPositionChangedOnTheBus_WhenTheChangeDetectorReturnsTrue()
        {
            var fixture = new AgvPositionRegistryTestFixture();
            fixture.ChangeDetectorMock.Setup(c => c.HasChanged(It.IsAny<AgvPositionStatus>(), It.IsAny<AgvPositionStatus>()))
                .Returns(true);

            var agvPositionStatus = fixture.AgvPositionStatus;
            fixture.AgvPositionRegistry.UpdatePosition(agvPositionStatus);

            fixture.BusMock.Verify(b => b.Publish(It.Is<AgvPositionChanged>(o =>
                    o.NewAgvPosition == agvPositionStatus.AgvPosition &&
                    o.AgvId == agvPositionStatus.AgvId),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [TestMethod]
        public void UpdatePosition_DoesNotPublishAgvPositionChangedOnTheBus_WhenTheChangeDetectorReturnsFalse()
        {
            var fixture = new AgvPositionRegistryTestFixture();
            fixture.ChangeDetectorMock.Setup(c => c.HasChanged(It.IsAny<AgvPositionStatus>(), It.IsAny<AgvPositionStatus>()))
                .Returns(false);

            fixture.AgvPositionRegistry.UpdatePosition(fixture.AgvPositionStatus);

            fixture.BusMock.Verify(b => b.Publish(It.IsAny<AgvPositionStatus>(), default), Times.Never);
        }

        [TestMethod]
        public void GetPosition_ReturnsNull_WhenRegistryIsEmpty()
        {
            var fixture = new AgvPositionRegistryTestFixture();

            var retrievedPosition = fixture.AgvPositionRegistry.GetPosition(fixture.CorrelationId);
            retrievedPosition.ShouldBeNull();
        }
    }
}
