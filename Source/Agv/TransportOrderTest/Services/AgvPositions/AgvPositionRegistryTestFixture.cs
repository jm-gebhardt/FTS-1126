using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;
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
    public AgvPositionStatus AgvPositionStatus { get; }
    public AgvPositionRegistry AgvPositionRegistry { get; }
    public Mock<IChangeDetector<AgvPositionStatus>> ChangeDetectorMock { get; }

    public AgvPositionRegistryTestFixture()
    {
        ChangeDetectorMock = new Mock<IChangeDetector<AgvPositionStatus>>();
        AgvPositionRegistry = new AgvPositionRegistry(BusMock.Object, ChangeDetectorMock.Object);
        AgvPositionStatus = new AgvPositionStatus(CorrelationId, AgvId, _agvPosition);
    }

}