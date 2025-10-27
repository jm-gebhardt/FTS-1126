using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionGroupChangedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupChanged_SendsActionGroupChanged()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupChangedConsumer, IActionGroupPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness.Bus.Publish(new ActionGroupChanged(actionGroup, actionGroup.CorrelationId));
        
        await fixture.ShouldHavePublishedAsync<ActionGroupChanged>();
        await fixture.ShouldHaveConsumedAsync<ActionGroupChanged>();

        fixture.PublisherMock.Verify(h => h.SendActionGroupChangedAsync(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_LogEntryCreated_ValidData_SendsLogEntryCreated()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupChangedConsumer, IActionGroupPublisher>();
        var logEntryDto = new LogEntryDto { Id = Guid.NewGuid(), ActionGroupCorrelationId = Guid.NewGuid(), Category = Categories.Order, LogCode = LogCodes.OrderOperatorInteraction, Timestamp = DateTime.UtcNow, LogLevel = nameof(LogLevel.Information) };

        await fixture.PublishMessageAsync(new LogEntryCreated(logEntryDto));

        fixture.PublisherMock.Verify(h => h.SendLogEntryCreated(logEntryDto.ActionGroupCorrelationId.GetValueOrDefault(), It.IsAny<LogEntry>()), Times.Once);
    }
}