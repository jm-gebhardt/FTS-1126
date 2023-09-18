using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
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
    public Mock<IChangeDetector<AgvPositionStatus>> ChangeDetectorMock { get; }

    public AgvPositionRegistryTestFixture()
    {
        ChangeDetectorMock = new Mock<IChangeDetector<AgvPositionStatus>>();
        AgvPositionRegistry = new AgvPositionRegistry(BusMock.Object, ChangeDetectorMock.Object, new AgvPositionDictionaryProvider());
        AgvPositionStatus = new AgvPositionStatus(CorrelationId, AgvId, _agvPosition);
    }

}