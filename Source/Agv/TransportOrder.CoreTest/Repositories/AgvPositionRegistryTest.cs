using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Messages.Events.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class AgvPositionRegistryTest
{
    [TestMethod]
    public async Task UpdatePosition_AddsObject_WhenRegistryIsEmpty()
    {
        var fixture = new AgvPositionRegistryTestFixture();
        var agvPositionRegistry = fixture.AgvPositionRegistry;
        var agvPositionStatus = fixture.AgvPositionStatus;

        List<AgvPositionStatus> positions = agvPositionRegistry.GetAllPositions();

        positions.ShouldNotBeNull();
        positions.Count.ShouldBe(0);
        await agvPositionRegistry.UpdatePositionAsync(agvPositionStatus);

        positions = agvPositionRegistry.GetAllPositions();
        positions.Count.ShouldBe(1);
        positions.First().ShouldBe(agvPositionStatus);
    }

    [TestMethod]
    public async Task UpdatePosition_StoresNewPosition_WhenRegistryContainsSingleAgv()
    {
        var fixture = new AgvPositionRegistryTestFixture();
        var newAgvPosition = new Common.Messages.AgvManagement.AgvPosition(true, 2, 3, 4, 0);
        var newAgvPositionUpdated = new AgvPositionStatus(fixture.CorrelationId, fixture.AgvId, newAgvPosition);
        var agvPositionStatus = fixture.AgvPositionStatus;
        var agvPositionRegistry = fixture.AgvPositionRegistry;

        await agvPositionRegistry.UpdatePositionAsync(agvPositionStatus);

        List<AgvPositionStatus> positions = agvPositionRegistry.GetAllPositions();

        positions.Count.ShouldBe(1);
        positions.First().ShouldBe(agvPositionStatus);

        await agvPositionRegistry.UpdatePositionAsync(newAgvPositionUpdated);

        positions = agvPositionRegistry.GetAllPositions();
        positions.Count.ShouldBe(1);
        positions.First().ShouldBe(newAgvPositionUpdated);
    }

    [TestMethod]
    public async Task UpdatePosition_PublishesAgvPositionChangedOnTheBus_WhenTheChangeDetectorReturnsTrue()
    {
        var fixture = new AgvPositionRegistryTestFixture();
        fixture.ChangeDetectorMock.Setup(c => c.HasChanged(It.IsAny<AgvPositionStatus>(), It.IsAny<AgvPositionStatus>()))
            .Returns(true);

        var agvPositionStatus = fixture.AgvPositionStatus;
        await fixture.AgvPositionRegistry.UpdatePositionAsync(agvPositionStatus);

        fixture.BusMock.Verify(b => b.Publish(It.Is<AgvPositionChanged>(o =>
                o.NewAgvPosition == agvPositionStatus.AgvPosition &&
                o.AgvId == agvPositionStatus.AgvId),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [TestMethod]
    public async Task UpdatePosition_DoesNotPublishAgvPositionChangedOnTheBus_WhenTheChangeDetectorReturnsFalse()
    {
        var fixture = new AgvPositionRegistryTestFixture();
        fixture.ChangeDetectorMock.Setup(c => c.HasChanged(It.IsAny<AgvPositionStatus>(), It.IsAny<AgvPositionStatus>()))
            .Returns(false);

        await fixture.AgvPositionRegistry.UpdatePositionAsync(fixture.AgvPositionStatus);

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