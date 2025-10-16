using AutoMapper;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.ActionGroup;

public class ActionGroupsControllerTestFixture : MockRequestClientControllerTestFixture<ActionGroupsController>
{
    private GotActionGroupStatistics _gotActionGroupStatistics = null!;
    private GotActionGroups _gotActionGroups = null!;
    private GotActionGroupCountsByState _gotActionGroupCountsByState = null!;
    private readonly GotFilteredActionGroups _gotFilteredActionGroups = null!;
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    protected override IEnumerable<Profile> GetProfiles() => [new ActionProfile()];

    protected override void SetupController()
    {
        AddServices(sc => sc.AddTransient(_ => _logEntryRepository.Object));
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        AddRequestClient<GetActionGroupStatistics, GotActionGroupStatistics>(() => _gotActionGroupStatistics);
        AddRequestClient<GetActionGroups, GotActionGroups>(() => _gotActionGroups);
        AddRequestClient<GetActionGroupCountsByState, GotActionGroupCountsByState>(() => _gotActionGroupCountsByState);
        AddRequestClient<GetFilteredActionGroups, GotFilteredActionGroups>(() => _gotFilteredActionGroups);
    }
    
    public new ActionGroupsControllerTestFixture Build()
    {
        base.Build();
        return this;
    }
    
    public ActionGroupsControllerTestFixture WithDurationStatistics(ICollection<DurationStatistics> durationStatistics)
    {
        _gotActionGroupStatistics = new GotActionGroupStatistics(durationStatistics);
        return this;
    }

    public ActionGroupsControllerTestFixture WithActionGroups(IReadOnlyCollection<ActionGroupReadModel> actionGroups)
    {
        _gotActionGroups = new GotActionGroups(actionGroups);
        return this;
    }

    public ActionGroupsControllerTestFixture WithActionGroupCount(IList<AgvGroupCountByState> groupCount)
    {
        _gotActionGroupCountsByState = new GotActionGroupCountsByState(groupCount);
        return this;
    }
}