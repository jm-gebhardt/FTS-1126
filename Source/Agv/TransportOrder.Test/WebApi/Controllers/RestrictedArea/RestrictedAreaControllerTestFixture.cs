using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using LogEntryProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.LogEntryProfile;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.RestrictedArea;

public class RestrictedAreaControllerTestFixture: MockRequestClientControllerTestFixture<RestrictedAreaController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new RestrictedAreaProfile(), new PolygonPointProfile(), new LogEntryProfile()];

    private GotRestrictedArea _gotRestrictedArea = null!;
    private RestrictedAreaAdded _restrictedAreaAdded = null!;

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
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync([]);
        AddRequestClient<GetRestrictedArea, GotRestrictedArea>(() => _gotRestrictedArea);
        AddRequestClient<AddRestrictedArea, RestrictedAreaAdded>(() => _restrictedAreaAdded);
        AddRequestClient<TryDeleteRestrictedArea, Success>(() => new Success());
        AddRequestClient<TryUpdateRestrictedArea, Success>(() => new Success());
    }

    public new RestrictedAreaControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public RestrictedAreaControllerTestFixture WithRestrictedArea(TransportOrder.Domain.Models.RestrictedArea.RestrictedArea restrictedArea)
    {
        _gotRestrictedArea = new GotRestrictedArea(restrictedArea);
        _restrictedAreaAdded = new RestrictedAreaAdded(restrictedArea);
        return this;
    }
}