using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.MaintenanceLog;

[TestClass]
public class MaintenanceLogCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_AddMaintenanceLog_ValidData_PublishesMaintenanceLogCreated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", agv.CorrelationId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, MaintenanceType.BatteryReplaced, null);
        var fixture = new MaintenanceLogCommandHandlerTestFixture()
            .WithLastMaintenance()
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new AddMaintenanceLog(maintenanceLog));

        await fixture.ShouldHavePublishedAsync<MaintenanceLogCreated>();
        await fixture.ShouldHavePublishedAsync<AgvMaintained>();
    }

    [TestMethod]
    public async Task Consume_AddMaintenanceLog_BatteryReplaced_SetsLastBatteryReplacement()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var executionDate = DateTime.UtcNow.AddDays(1);
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", agv.CorrelationId, executionDate, DateTime.UtcNow, MaintenanceType.BatteryReplaced, null);
        var fixture = new MaintenanceLogCommandHandlerTestFixture()
            .WithLastMaintenance()
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new AddMaintenanceLog(maintenanceLog));

        agv.AgvStatus!.LastBatteryReplacement.ShouldBe(executionDate);
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_AddMaintenanceLog_BatteryReplaced_BackdatedAfterOtherMaintenance_StillSetsLastBatteryReplacement()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var executionDate = DateTime.UtcNow.AddDays(1);
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", agv.CorrelationId, executionDate, DateTime.UtcNow, MaintenanceType.BatteryReplaced, null);
        // A newer maintenance of a different type already exists — its executionDate is later
        var newerMaintenance = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", agv.CorrelationId, executionDate.AddDays(5), DateTime.UtcNow, MaintenanceType.TiresReplaced, null);
        var fixture = new MaintenanceLogCommandHandlerTestFixture()
            .WithLastMaintenance(newerMaintenance)
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new AddMaintenanceLog(maintenanceLog));

        agv.AgvStatus!.LastBatteryReplacement.ShouldBe(executionDate);
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_AddMaintenanceLog_BatteryReplaced_BackdatedAfterNewerBatteryReplacement_DoesNotOverwrite()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var newerDate = new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc);
        agv.AgvStatus!.UpdateLastBatteryReplacement(newerDate);
        var backdatedLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", agv.CorrelationId, new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), DateTime.UtcNow, MaintenanceType.BatteryReplaced, null);
        var fixture = new MaintenanceLogCommandHandlerTestFixture()
            .WithLastMaintenance()
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new AddMaintenanceLog(backdatedLog));

        agv.AgvStatus!.LastBatteryReplacement.ShouldBe(newerDate);
        fixture.VerifyAgvUpdated(Times.Never());
    }

    [TestMethod]
    public async Task Consume_AddMaintenanceLog_CurrentMaintenanceOlderThanLast_DoesNotPublishAgvMaintained()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", agv.CorrelationId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, MaintenanceType.BatteryReplaced, null);
        var fixture = new MaintenanceLogCommandHandlerTestFixture()
            .WithLastMaintenance(new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", agv.CorrelationId, DateTime.UtcNow.AddHours(1), DateTime.UtcNow, MaintenanceType.BatteryReplaced, null))
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new AddMaintenanceLog(maintenanceLog));

        await fixture.ShouldHavePublishedAsync<MaintenanceLogCreated>();
        await fixture.ShouldNotHavePublishedAsync<AgvMaintained>();
    }
}