using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using LogEntryProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.LogEntryProfile;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Trigger;

public class TriggerControllerTestFixture : MockRequestClientControllerTestFixture<TriggerController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new TriggerProfile(), new PolygonPointProfile(), new LogEntryProfile()];

    private GotTrigger _gotTrigger = null!;
    private TriggerLocationAssigned _triggerLocationAssigned = null!;
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    protected override void SetupController()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _logEntryRepository.Object);
            s.AddTransient<ILoggingService, LoggingService>();
            s.AddTransient(_ => new Mock<IBus>().Object);
        });
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        AddRequestClient<GetTrigger, GotTrigger>(() => _gotTrigger);
        AddRequestClient<AssignTriggerToLocation, TriggerLocationAssigned>(() => _triggerLocationAssigned);
        AddRequestClient<TryRemoveTriggerLocation, Success>(() => new Success());
        AddRequestClient<TryUpdateDefaultConfigurationParameter, Success>(() => new Success());
        AddRequestClient<TryUpdateLocationConfigurationParameter, Success>(() => new Success());
        AddRequestClient<TryResetLocationConfigurationParameter, Success>(() => new Success());
    }

    public new TriggerControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public TriggerControllerTestFixture WithTrigger(TransportOrder.Domain.Models.Trigger.Trigger trigger)
    {
        _gotTrigger = new GotTrigger(trigger);
        return this;
    }

    public TriggerControllerTestFixture WithTriggerLocation(LocationDto locationDto, GraphNode? nodeId)
    {
        var mapper = MappingHelper.GetMapper(GetProfiles().ToArray());
        var location = mapper.Map<Location>(locationDto);
        _triggerLocationAssigned = new TriggerLocationAssigned(location, nodeId);
        return this;
    }
}