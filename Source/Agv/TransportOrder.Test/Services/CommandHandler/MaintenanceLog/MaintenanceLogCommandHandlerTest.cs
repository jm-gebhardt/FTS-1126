using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Test.Helper;

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