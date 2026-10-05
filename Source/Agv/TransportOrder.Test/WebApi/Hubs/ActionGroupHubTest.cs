using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

[TestClass]
public class ActionGroupHubTest
{
    [TestMethod]
    public async Task SendActionGroupCreatedAsync_SendsActionGroupChanged_To_ActionGroupCreated()
    {
        var fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new ActionGroupReadModel(Guid.NewGuid(), "order", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, ActionGroupState.Created, ActionGroupCreator.TransportOrder, Priority.Default, "hu", new List<AgvActionReadModel>(), false, false, true, new List<Reason>(), false);
        var expectedDto = Mock.Of<ActionGroupDto>();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<TransportOrder.Messages.MessageContent.Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendActionGroupCreatedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).ActionGroupCreated(It.IsAny<ActionGroupDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendActionGroupChangedAsync_SendsActionGroupChanged_To_ActionGroupChanged()
    {
        var fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new ActionGroupReadModel(Guid.NewGuid(), "order", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, ActionGroupState.Created, ActionGroupCreator.TransportOrder, Priority.Default, "hu", new List<AgvActionReadModel>(), false, false, true, new List<Reason>(), false);
        var expectedDto = Mock.Of<ActionGroupDto>();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<TransportOrder.Messages.MessageContent.Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendActionGroupChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).ActionGroupChanged(It.IsAny<ActionGroupDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendActionGroupChangedAsync_SendsActionGroupChangedWithReason_To_ActionGroupChanged()
    {
        var fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new ActionGroupReadModel(Guid.NewGuid(), "order", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, ActionGroupState.Paused, ActionGroupCreator.TransportOrder, Priority.Default, "hu", new List<AgvActionReadModel>(), false, false, true, new List<Reason>() { new("Maintenance") }, false);
        var expectedDto = Mock.Of<ActionGroupDto>();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<TransportOrder.Messages.MessageContent.Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendActionGroupChangedAsync(data);
        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).ActionGroupChanged(It.IsAny<ActionGroupDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendLogEntryCreated_SendsLogEntryDto_BroadCastOrderLogCreated()
    {
        var fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new LogEntry(LogCodes.OrderOperatorInteraction, Categories.Order, nameof(LogLevel.Information), DateTime.UtcNow, null, null, Guid.NewGuid());

        var expectedDto = Mock.Of<LogEntryCreatedDto>();
        fixture.MapperMock.Setup(m => m.Map<LogEntryCreatedDto>(It.IsAny<LogEntry>())).Returns(expectedDto);

        await fixture.Publisher.SendLogEntryCreated(data.ActionGroupCorrelationId.GetValueOrDefault(), data);
        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).OrderLogCreated(It.IsAny<LogEntryCreatedDto>()), Times.Once);
    }
}