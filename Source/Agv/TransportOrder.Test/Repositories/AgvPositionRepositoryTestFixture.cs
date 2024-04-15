using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;

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