using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.MaintenanceLog;

public class MaintenanceLogCommandHandlerTestFixture: MassTransitConsumerTestFixture<MaintenanceLogCommandHandler>
{
    protected override IEnumerable<Profile> GetProfiles() => [new AgvProfile(), new LogEntryProfile()];

    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    public MaintenanceLogCommandHandlerTestFixture()
    {
        _agvDb.Setup(x => x.MaintenanceLogs.AddAsync(It.IsAny<TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog>())).Verifiable();
        Build();
    }

    protected override MassTransitConsumerTestFixture<MaintenanceLogCommandHandler> AddServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient(_ => _logEntryRepository.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _, CancellationToken _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return this;
    }

    public MaintenanceLogCommandHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        _agvDb.Setup(r => r.Agvs.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<CancellationToken>())).ReturnsAsync(true).Verifiable();
        return this;
    }

    public void VerifyAgvUpdated(Times times)
    {
        _agvDb.Verify(x => x.Agvs.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<CancellationToken>()), times);
    }

    public MaintenanceLogCommandHandlerTestFixture WithLastMaintenance(TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog? maintenanceLog=null)
    {
        _agvDb.Setup(x => x.MaintenanceLogs.GetLatestMaintenanceLogForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(maintenanceLog);
        return this;
    }
}