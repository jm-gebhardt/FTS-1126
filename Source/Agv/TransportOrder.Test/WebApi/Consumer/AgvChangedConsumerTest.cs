using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class AgvChangedConsumerTest
{
    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Receiving_AgvOnlineChanged_SendAgvStateToAllClients(bool alive)
    {
        var agv = AgvGenerator.GenerateRawAgv(AgvSerialNumber.ToGuid(), AgvSerialNumber);
        agv.TransitionToDiscovered(true);
        agv.TransitionToIdle();
        agv.SetOnline();

        if (!alive)
        {
            agv.GoOffline();
        }

        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<Agv>(agv);

        var mockGetAgvsClient = new Mock<IRequestClient<GetAgvs>>();
        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));
        mockGetAgvsClient.Setup(
                m => m.GetResponse<GotAgvs>(It.IsAny<GetAgvs>(), CancellationToken.None, default
                ))
            .ReturnsAsync(responseMock.Object);

        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>(services => { services.AddSingleton(mockGetAgvsClient.Object); });
        await fixture.TestHarness.Bus.Publish(
            new AgvOnlineChanged(
                agv.CorrelationId,
                true
            ));

        (await fixture.ConsumerHarness.Consumed.Any<AgvOnlineChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendAgvOnlineChangedAsync(
            agv.CorrelationId, true), Times.Once);
    }

    [TestMethod]
    public async Task Consume_HasCargoChanged_ValidData_SentAgvHasCargoChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        const bool hasCargo = true;
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();

        await fixture.TestHarness.Bus.Publish(new HasCargoChanged(agvCorrelationId, hasCargo));
        (await fixture.ConsumerHarness.Consumed.Any<HasCargoChanged>()).ShouldBeTrue();
        fixture.PublisherMock.Verify(h => h.SendAgvHasCargoChangedAsync(agvCorrelationId, hasCargo),
            Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvModeChanged_ValidData_SentAgvModeChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();
      
        await fixture.TestHarness.Bus.Publish(new AgvModeChanged(agv.CorrelationId, agv.Mode));

        (await fixture.ConsumerHarness.Consumed.Any<AgvModeChanged>()).ShouldBeTrue();

        var mapper = GetMapper();
        var agvModeDto = mapper.Map<AgvModeDto>(agv.Mode);

        fixture.PublisherMock.Verify(h => h.SendAgvModeChangedAsync(agv.CorrelationId, agvModeDto),
            Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvStationsChanged_ValidData_SendAgvStationsChangedAsync()
    {
        var agvCorrelationId = Guid.NewGuid();
        ICollection<string> stations = ["station"];
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();

        await fixture.TestHarness.Bus.Publish(new AgvStationsChanged(agvCorrelationId, stations));
        (await fixture.ConsumerHarness.Consumed.Any<AgvStationsChanged>()).ShouldBeTrue();
        fixture.PublisherMock.Verify(h => h.SendAgvStationsChangedAsync(agvCorrelationId, stations),
            Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvMaintained_ValidData_SendAgvMaintainedAsync()
    {
        var agvCorrelationId = Guid.NewGuid();
        var maintenance = new Maintenance{ MaintenanceType = MaintenanceType.BatteryReplaced, ExecutionDate = DateTime.UtcNow };
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();

        await fixture.PublishMessageAsync(new AgvMaintained(agvCorrelationId, maintenance));

        fixture.PublisherMock.Verify(h => h.SendAgvMaintainedAsync(agvCorrelationId, maintenance), Times.Once);
    }

    [TestMethod]
    public async Task Consume_MaintenanceLogCreated_ValidData_SendMaintenanceLogCreated()
    {
        var agvCorrelationId = Guid.NewGuid();
        var log = new MaintenanceLog(Guid.NewGuid(), "user", agvCorrelationId, DateTime.UtcNow, DateTime.UtcNow, MaintenanceType.DrivesReplaced, null);
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();

        await fixture.PublishMessageAsync(new MaintenanceLogCreated(agvCorrelationId, log));

        fixture.PublisherMock.Verify(h => h.SendMaintenanceLogCreated(agvCorrelationId, log), Times.Once);
    }

    [TestMethod]
    public async Task Consume_LogEntryCreated_AgvLog_SendsLogEntryCreated()
    {
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();
        var logEntryDto = new LogEntryDto { Id = Guid.NewGuid(), AgvCorrelationId = Guid.NewGuid(), Category = Categories.Agv, LogCode = LogCodes.AgvOperatorInteraction, Timestamp = DateTime.UtcNow, LogLevel = nameof(LogLevel.Information) };

        await fixture.PublishMessageAsync(new LogEntryCreated(logEntryDto));

        fixture.PublisherMock.Verify(h => h.SendLogEntryCreated(logEntryDto.AgvCorrelationId.GetValueOrDefault(), It.IsAny<LogEntry>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_LogEntryCreated_BatteryLog_SendsLogEntryCreated()
    {
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();
        var logEntryDto = new LogEntryDto { Id = Guid.NewGuid(), AgvCorrelationId = Guid.NewGuid(), Category = Categories.Battery, LogCode = LogCodes.BatteryCriticalChargeLevel, Timestamp = DateTime.UtcNow, LogLevel = nameof(LogLevel.Information) };

        await fixture.PublishMessageAsync(new LogEntryCreated(logEntryDto));

        fixture.PublisherMock.Verify(h => h.SendBatteryLogEntryCreated(logEntryDto.AgvCorrelationId.GetValueOrDefault(), It.IsAny<LogEntry>()), Times.Once);
    }

    private static Mapper GetMapper()
    {
        return MappingHelper.GetMapper(new TransportOrder.WebApi.Profiles.AgvModeProfile(), new LogEntryProfile());
    }
}