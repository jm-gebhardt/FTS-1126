using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using LogEntryProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.LogEntryProfile;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Queue;

public class QueueControllerTestFixture : MockRequestClientControllerTestFixture<QueueController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new QueueProfile(), new LogEntryProfile()];

    private TransportOrder.Domain.Models.Queue.Queue _queue = null!;

    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    protected override void SetupController()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _logEntryRepository.Object);
            s.AddTransient<ILoggingService, LoggingService>();
            s.AddTransient(_ => _actionGroupRepository.Object);
            s.AddTransient(_ => new Mock<IBus>().Object);
        });
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<DateTime>(),
                It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync((Categories logCategory,
                int logCode,
                string logLevel,
                Guid? agvCorrelationId,
                Guid? actionGroupCorrelationId,
                Guid? stationId,
                DateTime timestamp,
                Dictionary<string, object?> _) => new LogEntry(logCode,
                logCategory,
                logLevel,
                timestamp,
                agvCorrelationId,
                stationId,
                actionGroupCorrelationId));
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync([]);
        AddRequestClient<DeleteQueue, Success>(() => new Success());
        AddRequestClient<GetQueue, GotQueue>(() => new GotQueue(_queue));
        AddRequestClient<TryAddQueue, GotQueue>(() => new GotQueue(_queue));
        AddRequestClient<RemoveAgvFromQueue, Success>(() => new Success());
    }

    public new QueueControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public QueueControllerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue queue)
    {
        _queue = queue;
        return this;
    }
}