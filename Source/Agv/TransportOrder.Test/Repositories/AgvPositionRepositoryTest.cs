using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class AgvPositionRepositoryTest
{
    [TestMethod]
    public async Task UpdatePosition_AddsObject_WhenRepositoryIsEmpty()
    {
        var fixture = new AgvPositionRepositoryTestFixture();
        var agvPositionRepository = fixture.AgvPositionRepository;
        var agvPositionStatus = AgvPositionStatusGenerator.Generate();

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
        var agvPositionStatus = AgvPositionStatusGenerator.Generate();
        var newAgvPositionUpdated = AgvPositionStatusGenerator.Generate(2, 3, 4, agvPositionStatus.CorrelationId);
        var fixture = new AgvPositionRepositoryTestFixture()
            .WithAgvPositionStatus(agvPositionStatus)
            .WithSignificanceCheckerReturns(true);
        var agvPositionRepository = fixture.AgvPositionRepository;

        List<AgvPositionStatus> positions = agvPositionRepository.GetAllPositions();
        positions.Count.ShouldBe(1);
        positions.First().ShouldBe(agvPositionStatus);

        await agvPositionRepository.UpdatePositionAsync(newAgvPositionUpdated);

        positions = agvPositionRepository.GetAllPositions();
        positions.Count.ShouldBe(1);
        positions.First().ShouldBe(newAgvPositionUpdated);
    }

    [TestMethod]
    public async Task UpdatePosition_PublishesAgvPositionChangedOnTheBus_WhenTheChangeCheckerReturnsTrue()
    {
        var fixture = new AgvPositionRepositoryTestFixture();

        var agvPositionStatus = AgvPositionStatusGenerator.Generate();
        await fixture.AgvPositionRepository.UpdatePositionAsync(agvPositionStatus);

        fixture.BusMock.Verify(b => b.Publish(It.Is<AgvPositionChanged>(o =>
                o.NewAgvPosition == agvPositionStatus.AgvPosition &&
                o.SerialNumber == agvPositionStatus.SerialNumber),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [TestMethod]
    public async Task UpdatePosition_NoChanges_DoesNotPublishAgvPositionChangedOnTheBus()
    {
        var agvPositionStatus = AgvPositionStatusGenerator.Generate();
        var fixture = new AgvPositionRepositoryTestFixture()
            .WithAgvPositionStatus(agvPositionStatus);

        fixture.AgvPositionDictionaryProvider.ConcurrentDict.TryAdd(agvPositionStatus.CorrelationId, agvPositionStatus);

        await fixture.AgvPositionRepository.UpdatePositionAsync(agvPositionStatus);

        fixture.BusMock.Verify(b => b.Publish(It.IsAny<AgvPositionStatus>(), default), Times.Never);
    }

    [TestMethod]
    public void GetPosition_ReturnsNull_WhenRepositoryIsEmpty()
    {
        var fixture = new AgvPositionRepositoryTestFixture();

        var retrievedPosition = fixture.AgvPositionRepository.GetPosition(Guid.NewGuid());
        retrievedPosition.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        false)] // positions are equal
    [DataRow(true, 0.0049f, 0.0049f, 0.0049f, 0.0049f, null,
        true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        false)] // positions are equal
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        false, 0.0f, 0.0f, 0.0f, 0.0f, null,
        true)] // positionInitialized mismatch
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        true, 0.0f, 0.0f, 0.0f, 0.0f, 0.005f,
        true)] // deviationRange difference within threshold
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, 0.0005f,
        true, 0.0f, 0.0f, 0.0f, 0.0f, 0.0015f,
        false)] // deviationRange difference is within threshold
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        true, 0.0051f, 0.0f, 0.0f, 0.0f, null,
        true)] // x difference is beyond threshold
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        true, 0.0f, 0.0051f, 0.0f, 0.0f, null,
        true)] // y difference is beyond threshold
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        true, 0.0f, 0.0f, 0.0051f, 0.0f, null,
        true)] // z difference is beyond threshold
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, null,
        true, 0.0f, 0.0f, 0.0f, 0.0051f, null,
        true)] // theta difference is beyond threshold
    [DataRow(true, 0.0f, 0.0f, 0.0f, 0.0f, 0.000f,
        true, 0.0f, 0.0f, 0.0f, 0.0051f, 0.0051f,
        true)] // deviationRange difference is beyond threshold
    public async Task UpdatePositionAsync_UpdatesRepository_WhenDataAreNotEqual(bool positionInitialized1, float x1, float y1, float z1, float theta1, float? deviationRange1,
        bool positionInitialized2, float x2, float y2, float z2, float theta2, float? deviationRange2,
        bool expectedUpdateCall)
    {
        var correlationId = Guid.NewGuid();
        var agvPositionStatus1 = AgvPositionStatusGenerator.Generate(x1, y1, z1, correlationId, theta1, positionInitialized1, deviationRange1);
        var agvPositionStatus2 = AgvPositionStatusGenerator.Generate(x2, y2, z2, correlationId, theta2, positionInitialized2, deviationRange2);

        var fixture = new AgvPositionRepositoryTestFixture()
            .WithAgvPositionStatus(agvPositionStatus1)
            .WithSignificanceCheckerReturns(expectedUpdateCall);

        await fixture.AgvPositionRepository.UpdatePositionAsync(agvPositionStatus2);

        fixture.AgvPositionDictionaryProvider.ConcurrentDict.TryGetValue(correlationId, out var position).ShouldBeTrue();

        switch (expectedUpdateCall)
        {
            case true:
                // was updated
                position.ShouldBe(agvPositionStatus2);
                break;

            case false:
                // was not updated
                position.ShouldBe(agvPositionStatus1);
                break;
        }
    }
}