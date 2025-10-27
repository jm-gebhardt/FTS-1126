using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.LogEntries;

[TestClass]
public class LogEntryCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_PersistLogEntry_ValidData_PublishesLogEntryCreated()
    {
        var fixture = new LogEntryCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new PersistLogEntry(new TransportOrder.Domain.Models.LogEntry.LogEntry(1, Categories.Agv, "Information", DateTime.MinValue, Guid.NewGuid(), null, null)));
        await fixture.ShouldHavePublishedAsync<LogEntryCreated>();
    }
}