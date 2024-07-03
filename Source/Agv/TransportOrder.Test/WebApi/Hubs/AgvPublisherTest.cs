using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

[TestClass]
public class AgvPublisherTest
{
    [TestMethod]
    public async Task SendAgvStateChangedAsync_SendsAgvStateDto_To_AgvStateChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var data = Mock.Of<Agv>();
        var expectedDto = Mock.Of<AgvStateDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvStateDto>(It.IsAny<Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvStateChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvStateChanged(It.IsAny<AgvStateDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvStateAsync_SendsAgvDto_To_AgvDiscovered()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var data = Mock.Of<Agv>();
        var expectedDto = Mock.Of<AgvDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvDto>(It.IsAny<Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvDiscoveredAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvDiscovered(It.IsAny<AgvDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvPositionAsync_SendsAgvPositionStatusDto_To_AgvPositionChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>()
            .Build();
        var data = new AgvPositionStatus(Guid.NewGuid(), "agv", new AgvPosition(true, 2, 3, 4, 5, 6));
        var expectedDto = new AgvPositionStatusDto(Guid.NewGuid(), "agv", new AgvPositionDto(true, 2, 3, 4, 5, 6));

        fixture.MapperMock.Setup(m => m.Map<AgvPositionStatusDto>(It.IsAny<Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvPositionAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvPositionChanged(It.IsAny<AgvPositionStatusDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvRouteAsync_SendsAgvRouteDto_To_AgvRouteChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var data = new CurrentAgvRouteReadModel(Guid.NewGuid(), Array.Empty<string>(), Array.Empty<Guid>());
        var expectedDto = Mock.Of<CurrentAgvRouteDto>();

        fixture.MapperMock.Setup(m => m.Map<CurrentAgvRouteDto>(It.IsAny<CurrentAgvRouteReadModel>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvRouteChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvRouteChanged(It.IsAny<CurrentAgvRouteDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAssignedActionGroupChangedAsync_SendsAssignedActionGroupDto_To_AssignedActionGroupChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        AssignedActionGroupDto? capturedDto = null;
        fixture.HubContextMock.Setup(c => c.Clients.All.AssignedActionGroupChanged(It.IsAny<AssignedActionGroupDto>()))
            .Callback<AssignedActionGroupDto>(dto => capturedDto = dto);

        var correlationId = Guid.NewGuid();
        var assignedActionGroup = new Common.Messages.ActionGroups.ActionGroup();
        var actionGroupDto = new ActionGroupDto();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<Common.Messages.ActionGroups.ActionGroup>())).Returns(actionGroupDto);

        await fixture.Publisher.SendAssignedActionGroupChangedAsync(correlationId, assignedActionGroup.CorrelationId);

        fixture.HubContextMock.Verify(c => c.Clients.All.AssignedActionGroupChanged(It.IsAny<AssignedActionGroupDto>()), Times.Once);
        capturedDto.ShouldNotBeNull();
        capturedDto.CorrelationId.ShouldBe(correlationId);
        capturedDto.ActionGroupCorrelationId.ShouldBe(actionGroupDto.CorrelationId);
    }

    [TestMethod]
    public async Task SendAgvBatteryChargeChangedAsync_SendsAgvBatteryStatusDto_To_BatteryStatusChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var data = Mock.Of<AgvBatteryChargeChanged>();
        var expectedDto = Mock.Of<AgvBatteryStatusDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvBatteryStatusDto>(It.IsAny<AgvBatteryChargeChanged>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvBatteryStatusAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.BatteryStatusChanged(It.IsAny<AgvBatteryStatusDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvBatteryStatusAsync_SendsAgvBatteryStatusDto_To_BatteryStatusChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var batteryChargeChanged = new AgvBatteryChargeChanged();
        var expectedDto = Mock.Of<AgvBatteryStatusDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvBatteryStatusDto>(It.IsAny<AgvBatteryChargeChanged>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvBatteryStatusAsync(batteryChargeChanged);

        fixture.HubContextMock.Verify(c => c.Clients.All.BatteryStatusChanged(It.Is<AgvBatteryStatusDto>(dto => dto == expectedDto)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvLockedChangedAsync_SendsAgvLockedStatusDto_To_AgvLockedChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var correlationId = Guid.NewGuid();
        const bool isLocked = true;

        await fixture.Publisher.SendAgvLockedChangedAsync(correlationId, isLocked);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvLockedChanged(It.Is<AgvLockedStatusDto>(dto => dto.CorrelationId == correlationId && dto.IsNowLocked == isLocked)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvOnlineChangedAsync_SendsAgvOnlineStatusDto_To_AgvOnlineChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var correlationId = Guid.NewGuid();
        const bool isOnline = true;

        await fixture.Publisher.SendAgvOnlineChangedAsync(correlationId, isOnline);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvOnlineChanged(It.Is<AgvOnlineStatusDto>(dto => dto.CorrelationId == correlationId && dto.IsNowOnline == isOnline)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvErrorChangedAsync_SendsAgvErrorStatusDto_To_AgvErrorChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var correlationId = Guid.NewGuid();
        var errors = new List<AgvError>();
        var expectedDto = Mock.Of<AgvErrorStatusDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvErrorStatusDto>(It.IsAny<List<AgvError>>())).Returns(expectedDto);
        fixture.MapperMock.Setup(m => m.Map(correlationId, expectedDto)).Returns(expectedDto);

        await fixture.Publisher.SendAgvErrorChangedAsync(correlationId, errors);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvErrorChanged(It.Is<AgvErrorStatusDto>(dto => dto == expectedDto)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvRemovedAsync_SendsAgvRemovedDto_To_AgvRemoved()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var correlationId = Guid.NewGuid();

        await fixture.Publisher.SendAgvRemovedAsync(correlationId);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvRemoved(It.Is<AgvRemovedDto>(dto => dto.CorrelationId == correlationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendCargoChangeAsync_SendsCargoDtoCollection_To_CargoChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        var expectedDtoCollection = new List<CargoDto>();

        fixture.MapperMock.Setup(m => m.Map<ICollection<CargoDto>>(It.IsAny<IReadOnlyCollection<Cargo>>())).Returns(expectedDtoCollection);

        await fixture.Publisher.SendCargoChangeAsync(new List<Cargo>());

        fixture.HubContextMock.Verify(c => c.Clients.All.CargoChanged(It.Is<ICollection<CargoDto>>(dtoCollection => dtoCollection == expectedDtoCollection)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvHasCargoChanged_ValidData_SendAgvHasCargoDto()
    {
        var agvCorrelationId = Guid.NewGuid();
        const bool hasCargo = true;
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>().Build();
        await fixture.Publisher.SendAgvHasCargoChangedAsync(agvCorrelationId, hasCargo);
        fixture.HubContextMock.Verify(c => c.Clients.All.AgvHasCargoChanged(It.Is<AgvHasCargoDto>(dto => dto.CorrelationId == agvCorrelationId && dto.HasCargo == hasCargo)), Times.Once);
    }
}