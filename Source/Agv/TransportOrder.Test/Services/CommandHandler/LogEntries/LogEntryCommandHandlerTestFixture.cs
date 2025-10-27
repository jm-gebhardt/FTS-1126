using AutoMapper;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.LogEntries;

public class LogEntryCommandHandlerTestFixture: MassTransitConsumerTestFixture<LogEntryCommandHandler>
{
    protected override IEnumerable<Profile> GetProfiles() => [new AgvProfile(), new LogEntryProfile()];

    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    public LogEntryCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<LogEntryCommandHandler> AddServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient(_ => _logEntryRepository.Object);
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new TransportOrder.Domain.Models.LogEntry.LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        return this;
    }
}