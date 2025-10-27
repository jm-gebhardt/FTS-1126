using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.System;
using Microsoft.Testing.Platform.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class SystemChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_LogEntryCreated_ValidData_SendsLogEntryCreated()
    {
        var fixture = new ConsumerHubTestFixture<SystemChangedConsumer, ISystemPublisher>();
        var logEntryDto = new LogEntryDto { Id = Guid.NewGuid(), Category = Categories.System, LogCode = LogCodes.SystemOperatorInteraction, Timestamp = DateTime.UtcNow, LogLevel = nameof(LogLevel.Information) };

        await fixture.PublishMessageAsync(new LogEntryCreated(logEntryDto));

        fixture.PublisherMock.Verify(h => h.SendLogEntryCreated(It.IsAny<LogEntry>()), Times.Once);
    }
}