using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfMaintenanceLogRepositoryTestFixture : TransportOrderDbFixture
{
    public EfMaintenanceLogRepository MaintenanceLogRepository { get; }

    public EfMaintenanceLogRepositoryTestFixture()
    {
        MaintenanceLogRepository = new EfMaintenanceLogRepository(ContextFactory);
    }

    [PublicAPI]
    public EfMaintenanceLogRepositoryTestFixture WithMaintenanceLog(MaintenanceLog maintenanceLog)
    {
        AddInitTask(MaintenanceLogRepository.AddAsync(maintenanceLog));
        return this;
    }

    public EfMaintenanceLogRepositoryTestFixture WithMaintenanceLogs(ICollection<MaintenanceLog> maintenanceLogs)
    {
        foreach (var log in maintenanceLogs)
        {
            WithMaintenanceLog(log);
        }
        return this;
    }

    private new void Build()
    {
        base.Build();
    }

    public new async Task<EfMaintenanceLogRepositoryTestFixture> BuildAsync()
    {
        await base.BuildAsync();
        Build();
        return this;
    }
}