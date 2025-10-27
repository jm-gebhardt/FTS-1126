using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

[TestClass]
public class SystemPublisherTest
{
    [TestMethod]
    public async Task SendLogEntryCreated_SendsLogEntryDto_BroadCastSystemLogCreated()
    {
        PublisherFixture<SystemPublisher, SystemHub, ISystemClient> fixture = new PublisherFixture<SystemPublisher, SystemHub, ISystemClient>().Build();
        var data = new LogEntry(LogCodes.SystemOperatorInteraction, Categories.System, nameof(LogLevel.Information), DateTime.UtcNow, null, null, null);

        var expectedDto = Mock.Of<LogEntryCreatedDto>();
        fixture.MapperMock.Setup(m => m.Map<LogEntryCreatedDto>(It.IsAny<LogEntry>())).Returns(expectedDto);

        await fixture.Publisher.SendLogEntryCreated(data);
        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).SystemLogCreated(It.IsAny<LogEntryCreatedDto>()), Times.Once);
    }
}