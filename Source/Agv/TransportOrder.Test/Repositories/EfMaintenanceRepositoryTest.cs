using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfMaintenanceRepositoryTest
{
    [TestMethod]
    public async Task GetMaintenancesLogsAsync_QueryByAgvId_ReturnsMaintenanceLogs()
    {
        var agv1 = Guid.NewGuid();
        var agv2 = Guid.NewGuid();
        var tireMaintenanceLogAgv1 = new MaintenanceLog("user", agv1, DateTime.UtcNow, MaintenanceType.TiresReplaced);
        var sensorsCleanedMaintenanceLogAgv1 = new MaintenanceLog("user", agv1, DateTime.UtcNow, MaintenanceType.SensorsCleaned);
        var tireMaintenanceLogAgv2 = new MaintenanceLog("user", agv2, DateTime.UtcNow, MaintenanceType.TiresReplaced);

        var fixture = await new EfMaintenanceLogRepositoryTestFixture()
            .WithMaintenanceLogs([tireMaintenanceLogAgv1, sensorsCleanedMaintenanceLogAgv1, tireMaintenanceLogAgv2])
            .BuildAsync();

        (await fixture.MaintenanceLogRepository.GetMaintenancesLogsAsync([agv1])).ShouldBe(new List<MaintenanceLog> { tireMaintenanceLogAgv1, sensorsCleanedMaintenanceLogAgv1 }, ignoreOrder:true);
    }

    [TestMethod]
    public async Task GetMaintenancesLogsAsync_QueryByType_ReturnsMaintenanceLogs()
    {
        var agv1 = Guid.NewGuid();
        var agv2 = Guid.NewGuid();
        var tireMaintenanceLogAgv1 = new MaintenanceLog("user", agv1, DateTime.UtcNow, MaintenanceType.TiresReplaced);
        var sensorsCleanedMaintenanceLogAgv1 = new MaintenanceLog("user", agv1, DateTime.UtcNow, MaintenanceType.SensorsCleaned);
        var tireMaintenanceLogAgv2 = new MaintenanceLog("user", agv2, DateTime.UtcNow, MaintenanceType.TiresReplaced);

        var fixture = await new EfMaintenanceLogRepositoryTestFixture()
            .WithMaintenanceLogs([tireMaintenanceLogAgv1, sensorsCleanedMaintenanceLogAgv1, tireMaintenanceLogAgv2])
            .BuildAsync();

        (await fixture.MaintenanceLogRepository.GetMaintenancesLogsAsync(maintenanceType: MaintenanceType.TiresReplaced)).ShouldBe(new List<MaintenanceLog> { tireMaintenanceLogAgv1, tireMaintenanceLogAgv2 }, ignoreOrder:true);
    }

    [TestMethod]
    public async Task GetMaintenancesLogsAsync_QueryByAgvIdAndType_ReturnsMaintenanceLogs()
    {
        var agv1 = Guid.NewGuid();
        var agv2 = Guid.NewGuid();
        var tireMaintenanceLogAgv1 = new MaintenanceLog("user", agv1, DateTime.UtcNow, MaintenanceType.TiresReplaced);
        var sensorsCleanedMaintenanceLogAgv1 = new MaintenanceLog("user", agv1, DateTime.UtcNow, MaintenanceType.SensorsCleaned);
        var tireMaintenanceLogAgv2 = new MaintenanceLog("user", agv2, DateTime.UtcNow, MaintenanceType.TiresReplaced);

        var fixture = await new EfMaintenanceLogRepositoryTestFixture()
            .WithMaintenanceLogs([tireMaintenanceLogAgv1, sensorsCleanedMaintenanceLogAgv1, tireMaintenanceLogAgv2])
            .BuildAsync();

        (await fixture.MaintenanceLogRepository.GetMaintenancesLogsAsync([agv1], MaintenanceType.TiresReplaced)).ShouldBeEquivalentTo(new List<MaintenanceLog> { tireMaintenanceLogAgv1 });
    }

    [TestMethod]
    public async Task GetMaintenancesLogsAsync_NoFilter_ReturnsAllMaintenanceLogs()
    {
        var agv1 = Guid.NewGuid();
        var agv2 = Guid.NewGuid();
        var tireMaintenanceLogAgv1 = new MaintenanceLog("user", agv1, DateTime.UtcNow, MaintenanceType.TiresReplaced);
        var sensorsCleanedMaintenanceLogAgv1 = new MaintenanceLog("user", agv1, DateTime.UtcNow, MaintenanceType.SensorsCleaned);
        var tireMaintenanceLogAgv2 = new MaintenanceLog("user", agv2, DateTime.UtcNow, MaintenanceType.TiresReplaced);

        var fixture = await new EfMaintenanceLogRepositoryTestFixture()
            .WithMaintenanceLogs([tireMaintenanceLogAgv1, sensorsCleanedMaintenanceLogAgv1, tireMaintenanceLogAgv2])
            .BuildAsync();

        (await fixture.MaintenanceLogRepository.GetMaintenancesLogsAsync()).ShouldBe(new List<MaintenanceLog> { tireMaintenanceLogAgv1, sensorsCleanedMaintenanceLogAgv1, tireMaintenanceLogAgv2 }, ignoreOrder: true);
    }

    [TestMethod]
    public async Task GetMaintenancesLogsAsync_NoData_ReturnsEmptyList()
    {
        var agv1 = Guid.NewGuid();
        var agv2 = Guid.NewGuid();

        var fixture = await new EfMaintenanceLogRepositoryTestFixture()
            .WithMaintenanceLogs([])
            .BuildAsync();

        (await fixture.MaintenanceLogRepository.GetMaintenancesLogsAsync([agv1, agv2], MaintenanceType.TiresReplaced)).ShouldBeEmpty();
    }
}