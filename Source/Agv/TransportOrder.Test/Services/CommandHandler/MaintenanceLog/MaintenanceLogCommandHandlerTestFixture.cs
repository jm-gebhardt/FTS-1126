using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.MaintenanceLog;

public class MaintenanceLogCommandHandlerTestFixture: MassTransitConsumerTestFixture<MaintenanceLogCommandHandler>
{
    protected override IEnumerable<Profile> GetProfiles() => [new AgvProfile()];

    private readonly Mock<IAgvDb> _agvDb = new();

    public MaintenanceLogCommandHandlerTestFixture()
    {
        _agvDb.Setup(x => x.MaintenanceLogs.AddAsync(It.IsAny<TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog>())).Verifiable();
        Build();
    }

    protected override MassTransitConsumerTestFixture<MaintenanceLogCommandHandler> AddServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _agvDb.Object);
        return this;
    }

    public MaintenanceLogCommandHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        return this;
    }

    public MaintenanceLogCommandHandlerTestFixture WithLastMaintenance(TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog? maintenanceLog=null)
    {
        _agvDb.Setup(x => x.MaintenanceLogs.GetLatestMaintenanceLogForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(maintenanceLog);
        return this;
    }
}