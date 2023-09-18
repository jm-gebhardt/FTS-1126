using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs;

[TestClass]
public class ActionGroupHubTest
{
    [TestMethod]
    public async Task SendActionGroupCreatedAsync_SendsActionGroupChanged_To_ActionGroupCreated()
    {
        HubFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient> fixture = new HubFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>()
            .WithMapper(new ActionProfile())
            .Build();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup().ConvertFromActionGroupWithFakeNodeType()!;

        await fixture.Publisher.SendActionGroupCreatedAsync(actionGroup);

        fixture.HubContextMock.Verify(c => c.Clients.All.ActionGroupCreated(It.Is<ActionGroupDto>(a => a.CorrelationId == actionGroup.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendActionGroupChangedAsync_SendsActionGroupChanged_To_ActionGroupChanged()
    {
        HubFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient> fixture = new HubFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>()
            .WithMapper(new ActionProfile())
            .Build();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup().ConvertFromActionGroupWithFakeNodeType()!;

        await fixture.Publisher.SendActionGroupChangedAsync(actionGroup);

        fixture.HubContextMock.Verify(c => c.Clients.All.ActionGroupChanged(It.Is<ActionGroupDto>(a => a.CorrelationId == actionGroup.CorrelationId)), Times.Once);
    }
}