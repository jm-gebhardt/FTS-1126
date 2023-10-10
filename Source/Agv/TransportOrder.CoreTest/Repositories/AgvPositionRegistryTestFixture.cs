using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

internal class AgvPositionRegistryTestFixture
{
    private readonly AgvPosition _agvPosition = new(true, 1, 2, 3, 0);

    public readonly Guid CorrelationId = Guid.NewGuid();
    public readonly string AgvId = "myAgv";

    public readonly Mock<IBus> BusMock = new();
    public AgvPositionStatus AgvPositionStatus { get; }
    public AgvPositionRegistry AgvPositionRegistry { get; }

    public AgvPositionDictionaryProvider AgvPositionDictionaryProvider { get; }

    public AgvPositionRegistryTestFixture()
    {
        AgvPositionDictionaryProvider = new AgvPositionDictionaryProvider();
        AgvPositionRegistry = new AgvPositionRegistry(BusMock.Object, AgvPositionDictionaryProvider);
        AgvPositionStatus = new AgvPositionStatus(CorrelationId, AgvId, _agvPosition);
    }

}