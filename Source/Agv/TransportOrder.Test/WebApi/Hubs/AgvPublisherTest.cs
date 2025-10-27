using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

[TestClass]
public class AgvPublisherTest
{
    [TestMethod]
    public async Task SendAgvStateChangedAsync_SendsAgvStateDto_To_AgvStateChanged()
    {
        var fixture = new AgvPublisherTestFixture();
        var data = Mock.Of<Agv>();
        var expectedDto = Mock.Of<AgvStateDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvStateDto>(It.IsAny<Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvStateChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvStateChanged(It.IsAny<AgvStateDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvStateAsync_SendsAgvDto_To_AgvDiscovered()
    {
        var fixture = new AgvPublisherTestFixture();
        var data = Mock.Of<Agv>();
        var expectedDto = Mock.Of<AgvDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvDto>(It.IsAny<Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvDiscoveredAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvDiscovered(It.IsAny<AgvDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvPositionAsync_SendsAgvPositionStatusDto_To_AgvPositionChanged()
    {
        var fixture = new AgvPublisherTestFixture();

        var data = new AgvPositionStatus(Guid.NewGuid(), "agv", new AgvPosition(true, 2, 3, 4, 5, 6));
        var expectedDto = new AgvPositionStatusDto(Guid.NewGuid(), "agv", new AgvPositionDto(true, 2, 3, 4, 5, 6));

        fixture.MapperMock.Setup(m => m.Map<AgvPositionStatusDto>(It.IsAny<Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvPositionAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvPositionChanged(It.IsAny<AgvPositionStatusDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvRouteAsync_SendsAgvRouteDto_To_AgvRouteChanged()
    {
        var fixture = new AgvPublisherTestFixture();
        var data = new CurrentAgvRouteReadModel(Guid.NewGuid(), [], []);
        var expectedDto = Mock.Of<CurrentAgvRouteDto>();

        fixture.MapperMock.Setup(m => m.Map<CurrentAgvRouteDto>(It.IsAny<CurrentAgvRouteReadModel>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvRouteChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvRouteChanged(It.IsAny<CurrentAgvRouteDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendAssignedActionGroupChangedAsync_SendsAssignedActionGroupDto_To_AssignedActionGroupChanged()
    {
        var fixture = new AgvPublisherTestFixture();
        AssignedActionGroupDto? capturedDto = null;
        fixture.HubContextMock.Setup(c => c.Clients.Client(fixture.TestConnectionId).AssignedActionGroupChanged(It.IsAny<AssignedActionGroupDto>()))
            .Callback<AssignedActionGroupDto>(dto => capturedDto = dto);

        var correlationId = Guid.NewGuid();
        var assignedActionGroup = new Common.Messages.ActionGroups.ActionGroup();
        var actionGroupDto = new ActionGroupDto();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<Common.Messages.ActionGroups.ActionGroup>())).Returns(actionGroupDto);

        await fixture.Publisher.SendAssignedActionGroupChangedAsync(correlationId, assignedActionGroup.CorrelationId);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AssignedActionGroupChanged(It.IsAny<AssignedActionGroupDto>()), Times.Once);
        capturedDto.ShouldNotBeNull();
        capturedDto.CorrelationId.ShouldBe(correlationId);
        capturedDto.ActionGroupCorrelationId.ShouldBe(actionGroupDto.CorrelationId);
    }

    [TestMethod]
    public async Task SendAgvBatteryStatusAsync_SendsAgvBatteryStatusDto_To_BatteryStatusChanged()
    {
        var fixture = new AgvPublisherTestFixture()
            .WithAgvSettings();
        var batteryStatusChanged = new AgvBatteryStatusChanged(Guid.Empty, new AgvBatteryStatus());
        var expectedDto = Mock.Of<AgvBatteryStatusDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvBatteryStatusDto>(It.IsAny<(AgvBatteryStatus, AgvSettingsReadModel)>())).Returns(expectedDto);

        await fixture.Publisher.SendAgvBatteryStatusAsync(batteryStatusChanged);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).BatteryStatusChanged(It.Is<AgvBatteryStatusDto>(dto => dto == expectedDto)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvBatteryStatusAsync_MapsSettingsOntoDto()
    {
        var correlationId = Guid.NewGuid();
        var batteryStatus = new AgvBatteryStatus
        {
            CorrelationId = correlationId,
            BatteryCharge = 50,
            BatteryVoltage = 24.0,
            BatteryCurrent = 5.0,
            BatteryTemperature = 30.0,
            EstimatedFullyChargedTime = DateTime.Now.AddHours(1),
            LastBatteryReplacement = DateTime.Now.AddDays(-30),
            IsCharging = true
        };
        var agvSettings = new AgvSettingsReadModel
        {
            BatteryLevelLow = 20,
            BatteryLevelCritical = 10,
            MinChargeUp = 5,
            MaxChargeUp = 100
        };
        var batteryStatusChanged = new AgvBatteryStatusChanged(correlationId, batteryStatus);
        AgvBatteryStatusDto? capturedDto = null;

        var fixture = new AgvPublisherTestFixture(true)
            .WithAgvSettings(agvSettings);
        fixture.HubContextMock.Setup(c => c.Clients.Client(fixture.TestConnectionId).BatteryStatusChanged(It.IsAny<AgvBatteryStatusDto>()))
            .Callback<AgvBatteryStatusDto>(dto => capturedDto = dto);

        await fixture.Publisher.SendAgvBatteryStatusAsync(batteryStatusChanged);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).BatteryStatusChanged(It.IsAny<AgvBatteryStatusDto>()), Times.Once);

        capturedDto.ShouldNotBeNull();
        capturedDto.ShouldSatisfyAllConditions(
            d => d.CorrelationId.ShouldBe(correlationId),
            d => d.BatteryCharge.ShouldBe(batteryStatus.BatteryCharge),
            d => d.BatteryVoltage.ShouldBe(batteryStatus.BatteryVoltage),
            d => d.BatteryCurrent.ShouldBe(batteryStatus.BatteryCurrent),
            d => d.BatteryTemperature.ShouldBe(batteryStatus.BatteryTemperature),
            d => d.EstimatedFullyChargedTime.ShouldBe(batteryStatus.EstimatedFullyChargedTime.GetValueOrDefault().ToUniversalTime()),
            d => d.LastBatteryReplacement.ShouldBe(batteryStatus.LastBatteryReplacement.GetValueOrDefault().ToUniversalTime()),
            d => d.IsCharging.ShouldBe(batteryStatus.IsCharging),
            d => d.BatteryLevelLow.ShouldBe(agvSettings.BatteryLevelLow),
            d => d.BatteryLevelCritical.ShouldBe(agvSettings.BatteryLevelCritical),
            d => d.MinChargeUp.ShouldBe(agvSettings.MinChargeUp),
            d => d.MaxChargeUp.ShouldBe(agvSettings.MaxChargeUp)
        );
    }

    [TestMethod]
    public async Task SendAgvLockedChangedAsync_SendsAgvLockedStatusDto_To_AgvLockedChanged()
    {
        var fixture = new AgvPublisherTestFixture();
        var correlationId = Guid.NewGuid();
        const bool isLocked = true;

        await fixture.Publisher.SendAgvLockedChangedAsync(correlationId, isLocked);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvLockedChanged(It.Is<AgvLockedStatusDto>(dto => dto.CorrelationId == correlationId && dto.IsNowLocked == isLocked)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvOnlineChangedAsync_SendsAgvOnlineStatusDto_To_AgvOnlineChanged()
    {
        var fixture = new AgvPublisherTestFixture();
        var correlationId = Guid.NewGuid();
        const bool isOnline = true;

        await fixture.Publisher.SendAgvOnlineChangedAsync(correlationId, isOnline);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvOnlineChanged(It.Is<AgvOnlineStatusDto>(dto => dto.CorrelationId == correlationId && dto.IsNowOnline == isOnline)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvErrorChangedAsync_SendsAgvErrorStatusDto_To_AgvErrorChanged()
    {
        var fixture = new AgvPublisherTestFixture();
        var correlationId = Guid.NewGuid();
        var errors = new List<AgvError>();
        var expectedDto = Mock.Of<AgvErrorStatusDto>();

        fixture.MapperMock.Setup(m => m.Map<AgvErrorStatusDto>(It.IsAny<List<AgvError>>())).Returns(expectedDto);
        fixture.MapperMock.Setup(m => m.Map(correlationId, expectedDto)).Returns(expectedDto);

        await fixture.Publisher.SendAgvErrorChangedAsync(correlationId, errors);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvErrorChanged(It.Is<AgvErrorStatusDto>(dto => dto == expectedDto)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvRemovedAsync_SendsAgvRemovedDto_To_AgvRemoved()
    {
        var fixture = new AgvPublisherTestFixture();
        var correlationId = Guid.NewGuid();

        await fixture.Publisher.SendAgvRemovedAsync(correlationId);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvRemoved(It.Is<AgvRemovedDto>(dto => dto.CorrelationId == correlationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendCargoChangeAsync_SendsCargoDtoCollection_To_CargoChanged()
    {
        var fixture = new AgvPublisherTestFixture();
        var expectedDtoCollection = new List<CargoDto>();

        fixture.MapperMock.Setup(m => m.Map<ICollection<CargoDto>>(It.IsAny<IReadOnlyCollection<Cargo>>())).Returns(expectedDtoCollection);

        await fixture.Publisher.SendCargoChangeAsync(new List<Cargo>());

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).CargoChanged(It.Is<ICollection<CargoDto>>(dtoCollection => dtoCollection == expectedDtoCollection)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvHasCargoChanged_ValidData_SendAgvHasCargoDto()
    {
        var agvCorrelationId = Guid.NewGuid();
        const bool hasCargo = true;
        var fixture = new AgvPublisherTestFixture();
        await fixture.Publisher.SendAgvHasCargoChangedAsync(agvCorrelationId, hasCargo);
        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvHasCargoChanged(It.Is<AgvHasCargoDto>(dto => dto.CorrelationId == agvCorrelationId && dto.HasCargo == hasCargo)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvZonesChanged_ValidData_SendAgvZonesChangedDto()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvPublisherTestFixture();
        await fixture.Publisher.SendAgvZonesChangedAsync(agvCorrelationId, ["test"]);
        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvZonesChanged(It.Is<AgvZonesDto>(dto => dto.CorrelationId == agvCorrelationId && dto.Zones.Contains("test"))));
    }

    [TestMethod]
    public async Task SendAgvStationsChanged_ValidData_SendAgvStationsChangedDto()
    {
        var agvCorrelationId = Guid.NewGuid();
        ICollection<string> stations = ["station"];
        var fixture = new AgvPublisherTestFixture();
        await fixture.Publisher.SendAgvStationsChangedAsync(agvCorrelationId, stations);
        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvStationsChanged(It.Is<AgvStationsDto>(dto => dto.CorrelationId == agvCorrelationId && dto.Stations.Contains(stations.Single()))));
    }

    [TestMethod]
    public async Task SendAgvMaintainedAsync_ValidData_BroadCastAgvMaintained()
    {
        var agvCorrelationId = Guid.NewGuid();
        var maintenance = new Maintenance { ExecutionDate = DateTime.UtcNow, MaintenanceType = MaintenanceType.BatteryReplaced };

        var fixture = new AgvPublisherTestFixture(useAutoMapper: true);

        await fixture.Publisher.SendAgvMaintainedAsync(agvCorrelationId, maintenance);
        fixture.HubContextMock.Verify(c =>
            c.Clients.Client(fixture.TestConnectionId).AgvMaintained(It.Is<AgvMaintainedDto>(dto =>
                dto.CorrelationId == agvCorrelationId && dto.Maintenance.ExecutionDate == maintenance.ExecutionDate && dto.Maintenance.MaintenanceType == maintenance.MaintenanceType)));
    }

    [TestMethod]
    public async Task SendMaintenanceLogCreated_ValidData_BroadCastAgvMaintenanceLogCreated()
    {
        var agvCorrelationId = Guid.NewGuid();
        var maintenanceLog = new MaintenanceLog(Guid.NewGuid(), "user", agvCorrelationId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, MaintenanceType.BatteryReplaced, null);
        var fixture = new AgvPublisherTestFixture(useAutoMapper: true);

        await fixture.Publisher.SendMaintenanceLogCreated(agvCorrelationId, maintenanceLog);
        fixture.HubContextMock.Verify(c =>
            c.Clients.Client(fixture.TestConnectionId).AgvMaintenanceLogCreated(It.Is<MaintenanceLogCreatedDto>(dto =>
                dto.AgvCorrelationId == agvCorrelationId && dto.MaintenanceLog.ExecutionDate == maintenanceLog.ExecutionDate && dto.MaintenanceLog.MaintenanceType == maintenanceLog.MaintenanceType)));
    }

    [TestMethod]
    public async Task SendLogEntryCreated_AgvLog_BroadCastAgvLogCreated()
    {
        var fixture = new AgvPublisherTestFixture(useAutoMapper: true);
        var data = new LogEntry(LogCodes.AgvOperatorInteraction, Categories.Agv, nameof(LogLevel.Information), DateTime.UtcNow, Guid.NewGuid(), null, null);

        await fixture.Publisher.SendLogEntryCreated(data.AgvCorrelationId.GetValueOrDefault(), data);
        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvLogCreated(It.IsAny<LogEntryCreatedDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendBatteryLogEntryCreated_BatteryLog_BroadCastBatteryLogCreated()
    {
        var fixture = new AgvPublisherTestFixture(useAutoMapper: true);
        var data = new LogEntry(LogCodes.BatteryCriticalChargeLevel, Categories.Battery, nameof(LogLevel.Information), DateTime.UtcNow, Guid.NewGuid(), null, null);

        await fixture.Publisher.SendBatteryLogEntryCreated(data.AgvCorrelationId.GetValueOrDefault(), data);
        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).BatteryLogCreated(It.IsAny<LogEntryCreatedDto>()), Times.Once);
    }
}