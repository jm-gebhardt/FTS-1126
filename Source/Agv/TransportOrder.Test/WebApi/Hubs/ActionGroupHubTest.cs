using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

[TestClass]
public class ActionGroupHubTest
{
    [TestMethod]
    public async Task SendActionGroupCreatedAsync_SendsActionGroupChanged_To_ActionGroupCreated()
    {
        PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient> fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new ActionGroupReadModel(Guid.NewGuid(), "order", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, ActionGroupState.Created, ActionGroupCreator.TransportOrder, "hu", new List<AgvActionReadModel>(), false, false, true, new List<Reason>());
        var expectedDto = Mock.Of<ActionGroupDto>();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<TransportOrder.Messages.MessageContent.Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendActionGroupCreatedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.ActionGroupCreated(It.IsAny<ActionGroupDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendActionGroupChangedAsync_SendsActionGroupChanged_To_ActionGroupChanged()
    {
        PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient> fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new ActionGroupReadModel(Guid.NewGuid(), "order", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, ActionGroupState.Created, ActionGroupCreator.TransportOrder, "hu", new List<AgvActionReadModel>(), false, false, true, new List<Reason>());
        var expectedDto = Mock.Of<ActionGroupDto>();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<TransportOrder.Messages.MessageContent.Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendActionGroupChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.ActionGroupChanged(It.IsAny<ActionGroupDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendActionGroupChangedAsync_SendsActionGroupChangedWithReason_To_ActionGroupChanged()
    {
        PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient> fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new ActionGroupReadModel(Guid.NewGuid(), "order", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, ActionGroupState.Paused, ActionGroupCreator.TransportOrder, "hu", new List<AgvActionReadModel>(), false, false, true, new List<Reason>() {new ("Maintenance")});
        var expectedDto = Mock.Of<ActionGroupDto>();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<TransportOrder.Messages.MessageContent.Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendActionGroupChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.ActionGroupChanged(It.IsAny<ActionGroupDto>()), Times.Once);
    }
}