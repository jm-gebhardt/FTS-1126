using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class AgvPositionRepositoryTest
{
    [TestMethod]
    public async Task UpdatePosition_AddsObject_WhenRepositoryIsEmpty()
    {
        var fixture = new AgvPositionRepositoryTestFixture();
        var agvPositionRepository = fixture.AgvPositionRepository;
        var agvPositionStatus = fixture.AgvPositionStatus;

        List<AgvPositionStatus> positions = agvPositionRepository.GetAllPositions();

        positions.ShouldNotBeNull();
        positions.Count.ShouldBe(0);
        await agvPositionRepository.UpdatePositionAsync(agvPositionStatus);

        positions = agvPositionRepository.GetAllPositions();
        positions.Count.ShouldBe(1);
        positions.First().ShouldBe(agvPositionStatus);
    }

    [TestMethod]
    public async Task UpdatePosition_StoresNewPosition_WhenRepositoryContainsSingleAgv()
    {
        var fixture = new AgvPositionRepositoryTestFixture();
        var newAgvPosition = new AgvPosition(true, 2, 3, 4, 0);
        var newAgvPositionUpdated = new AgvPositionStatus(fixture.CorrelationId, AgvPositionRepositoryTestFixture.AgvId, newAgvPosition);
        var agvPositionStatus = fixture.AgvPositionStatus;
        var agvPositionRepository = fixture.AgvPositionRepository;

        await agvPositionRepository.UpdatePositionAsync(agvPositionStatus);

        List<AgvPositionStatus> positions = agvPositionRepository.GetAllPositions();

        positions.Count.ShouldBe(1);
        positions.First().ShouldBe(agvPositionStatus);

        await agvPositionRepository.UpdatePositionAsync(newAgvPositionUpdated);

        positions = agvPositionRepository.GetAllPositions();
        positions.Count.ShouldBe(1);
        positions.First().ShouldBe(newAgvPositionUpdated);
    }

    [TestMethod]
    public async Task UpdatePosition_PublishesAgvPositionChangedOnTheBus_WhenTheChangeDetectorReturnsTrue()
    {
        var fixture = new AgvPositionRepositoryTestFixture();

        var agvPositionStatus = fixture.AgvPositionStatus;
        await fixture.AgvPositionRepository.UpdatePositionAsync(agvPositionStatus);

        fixture.BusMock.Verify(b => b.Publish(It.Is<AgvPositionChanged>(o =>
                o.NewAgvPosition == agvPositionStatus.AgvPosition &&
                o.AgvId == agvPositionStatus.AgvId),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [TestMethod]
    public async Task UpdatePosition_NoChanges_DoesNotPublishAgvPositionChangedOnTheBus()
    {
        var fixture = new AgvPositionRepositoryTestFixture();

        fixture.AgvPositionDictionaryProvider.ConcurrentDict.TryAdd(fixture.AgvPositionStatus.CorrelationId, fixture.AgvPositionStatus);

        await fixture.AgvPositionRepository.UpdatePositionAsync(fixture.AgvPositionStatus);

        fixture.BusMock.Verify(b => b.Publish(It.IsAny<AgvPositionStatus>(), default), Times.Never);
    }

    [TestMethod]
    public void GetPosition_ReturnsNull_WhenRepositoryIsEmpty()
    {
        var fixture = new AgvPositionRepositoryTestFixture();

        var retrievedPosition = fixture.AgvPositionRepository.GetPosition(fixture.CorrelationId);
        retrievedPosition.ShouldBeNull();
    }
}