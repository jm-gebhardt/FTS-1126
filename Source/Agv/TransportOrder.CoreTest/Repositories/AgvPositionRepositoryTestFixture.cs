using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

internal class AgvPositionRepositoryTestFixture
{
    private readonly Mock<ISignificanceChecker<AgvPosition?>> _significanceChecker = new();
    public readonly Mock<IBus> BusMock = new();

    public AgvPositionRepository AgvPositionRepository { get; }

    public AgvPositionDictionaryProvider AgvPositionDictionaryProvider { get; } = new();

    public AgvPositionRepositoryTestFixture()
    {
        AgvPositionRepository = new AgvPositionRepository(BusMock.Object, AgvPositionDictionaryProvider, _significanceChecker.Object);
    }

    public AgvPositionRepositoryTestFixture WithSignificanceCheckerReturns(bool returnValue)
    {
        _significanceChecker.Setup(s => s.HasSignificantChanges(It.IsAny<AgvPosition?>(), It.IsAny<AgvPosition?>())).Returns(returnValue);
        return this;
    }

    public AgvPositionRepositoryTestFixture WithAgvPositionStatus(AgvPositionStatus agvPositionStatus)
    {
        AgvPositionDictionaryProvider.ConcurrentDict.AddOrUpdate(agvPositionStatus.CorrelationId, agvPositionStatus, (_, _) => agvPositionStatus);
        return this;
    }
}