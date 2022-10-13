using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services.AgvPositions;

internal class AgvPositionRegistryTestFixture
{
    private readonly Common.Messages.AgvManagement.AgvPosition _agvPosition = new(true, 1, 2, 3, 0);

    public readonly Guid CorrelationId = Guid.NewGuid();
    public readonly string AgvId = "myAgv";

    public readonly Mock<IBus> BusMock = new();
    public AgvPositionReceived AgvPositionReceived { get; }
    public AgvPositionRegistry AgvPositionRegistry { get; }
    public Mock<IChangeDetector<AgvPositionReceived>> ChangeDetectorMock { get; }

    public AgvPositionRegistryTestFixture()
    {
        ChangeDetectorMock = new Mock<IChangeDetector<AgvPositionReceived>>();
        AgvPositionRegistry = new AgvPositionRegistry(BusMock.Object, ChangeDetectorMock.Object);
        AgvPositionReceived = new AgvPositionReceived(CorrelationId, AgvId, _agvPosition);
    }

}