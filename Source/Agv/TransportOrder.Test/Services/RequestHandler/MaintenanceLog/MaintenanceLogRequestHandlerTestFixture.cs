using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.MaintenanceLog;

public class MaintenanceLogRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, MockConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IMaintenanceLogRepository> _maintenanceLogRepository = new();

    public MaintenanceLogRequestHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _maintenanceLogRepository.Object);
        });
        base.Build();
    }

    public MaintenanceLogRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvs(ICollection<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDb.AddAgvs(agvs.ToList());
        return this;
    }

    public MaintenanceLogRequestHandlerTestFixture<TRequestClient, TConsumer> WithMaintenanceLogs(ICollection<TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog> logs)
    {
        _maintenanceLogRepository.Setup(x => x.GetMaintenancesLogsAsync(It.IsAny<ICollection<Guid>>(), It.IsAny<MaintenanceType?>())).ReturnsAsync(logs.ToList());
        return this;
    }
}