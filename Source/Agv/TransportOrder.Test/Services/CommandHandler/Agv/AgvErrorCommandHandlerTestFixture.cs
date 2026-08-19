using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Notification;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Agv;

public class AgvErrorCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvErrorCommandHandler>
{
    public readonly Mock<IAgvDb> AgvDbMock = new();
    private readonly List<AgvError> _errors = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<ITransportOrderNotificationSender> _toNotificationSender = new();
    private readonly Mock<INotificationManager> _notManager = new();

    protected override IEnumerable<Profile> GetProfiles() => [new LogEntryProfile()];

    public AgvErrorCommandHandlerTestFixture()
    {
        AgvDbMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(() => _errors);
        AgvDbMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(() => _errors);
        AgvDbMock.Setup(x => x.Errors.AddAndUpdateAgvErrorAsync(It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>(), It.IsAny<Guid>())).Callback((List<AgvError> newErrors, List<AgvError> resolvedErrors, Guid agvCorrelationId) =>
        {
            _errors.RemoveAll(e => e.AgvCorrelationId == agvCorrelationId && resolvedErrors.Any(r => r.CorrelationId == e.CorrelationId));
            _errors.AddRange(newErrors);
        });
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvErrorCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => AgvDbMock.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient(_ => _logEntryRepository.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddSingleton<ITransportOrderNotificationSender>(_ => _toNotificationSender.Object);
        collection.AddSingleton<INotificationManager>(_ => _notManager.Object);
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        AgvDbMock.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }
    
    public AgvErrorCommandHandlerTestFixture WithActionGroups(IReadOnlyList<TransportOrder.Domain.Models.ActionGroup.ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(actionGroups);
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithActiveAgvErrors(IReadOnlyList<AgvError> agvErrors)
    {
        _errors.AddRange(agvErrors);
        return this;
    }
}