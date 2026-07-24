using AutoMapper;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Queue;

public class ResendActionRequestHandlerTestFixture<TMessage> : MediatorTestFixture<TMessage, ResendActionRequestHandler, MockConsumer>
    where TMessage : class
{
    private readonly Mock<IAgvDb> _agvDb = new();

    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    protected override IEnumerable<Profile> GetProfiles() => [new Gebhardt.Acc.TransportOrder.Domain.Profiles.ActionProfile(), new Gebhardt.Acc.TransportOrder.Domain.Profiles.LogEntryProfile()];

    protected override ResendActionRequestHandlerTestFixture<TMessage> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient(_ => _logEntryRepository.Object);
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), 
            It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, 
                           Guid? stationId, DateTime timestamp, Dictionary<string, object?> _, CancellationToken _) 
                => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));

        return this;
    }

    public new ResendActionRequestHandlerTestFixture<TMessage> Build()
    {
        base.Build();
        return this;
    }

    public ResendActionRequestHandlerTestFixture<TMessage> AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDb.AddAgvs(agvs);
        return this;
    }

    public ResendActionRequestHandlerTestFixture<TMessage> AddActionGroup(Common.Messages.ActionGroups.ActionGroup? actionGroup)
    {
        var ag = Mapper.Map<TransportOrder.Domain.Models.ActionGroup.ActionGroup>(actionGroup);
        _actionGroupRepository.Setup(x => x.GetCurrentAssignedActionGroupAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ag);

        return this;
    }
}