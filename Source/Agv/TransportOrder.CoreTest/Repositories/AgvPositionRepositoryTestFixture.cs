using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

internal class AgvPositionRepositoryTestFixture
{
    public readonly Mock<IBus> BusMock = new();

    public AgvPositionRepository AgvPositionRepository { get; }

    public AgvPositionDictionaryProvider AgvPositionDictionaryProvider { get; } = new();

    public AgvPositionRepositoryTestFixture()
    {
        AgvPositionRepository = new AgvPositionRepository(BusMock.Object, AgvPositionDictionaryProvider);
    }

    public AgvPositionRepositoryTestFixture WithAgvPositionStatus(AgvPositionStatus agvPositionStatus)
    {
        AgvPositionDictionaryProvider.ConcurrentDict.AddOrUpdate(agvPositionStatus.CorrelationId, agvPositionStatus, (_, _) => agvPositionStatus);
        return this;
    }
}