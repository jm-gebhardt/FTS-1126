using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs;

[TestClass]
public class ActionGroupHubTest
{
    [TestMethod]
    public async Task SendActionGroupCreatedAsync_SendsActionGroupChanged_To_ActionGroupCreated()
    {
        PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient> fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new ActionGroupReadModel(Guid.NewGuid(), "order", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, ActionGroupState.Created, ActionGroupCreator.TransportOrder, new List<AgvActionReadModel>(), false, new List<Reason>());
        var expectedDto = Mock.Of<ActionGroupDto>();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<Messages.MessageContent.Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendActionGroupCreatedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.ActionGroupCreated(It.IsAny<ActionGroupDto>()), Times.Once);
    }

    [TestMethod]
    public async Task SendActionGroupChangedAsync_SendsActionGroupChanged_To_ActionGroupChanged()
    {
        PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient> fixture = new PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>().Build();
        var data = new ActionGroupReadModel(Guid.NewGuid(), "order", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, ActionGroupState.Created, ActionGroupCreator.TransportOrder, new List<AgvActionReadModel>(), false, new List<Reason>());
        var expectedDto = Mock.Of<ActionGroupDto>();

        fixture.MapperMock.Setup(m => m.Map<ActionGroupDto>(It.IsAny<Messages.MessageContent.Agv>())).Returns(expectedDto);

        await fixture.Publisher.SendActionGroupChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.ActionGroupChanged(It.IsAny<ActionGroupDto>()), Times.Once);
    }
}