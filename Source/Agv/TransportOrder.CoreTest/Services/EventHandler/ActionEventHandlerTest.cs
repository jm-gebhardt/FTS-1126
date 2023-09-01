using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using TransportOrder.Messages.Commands.ActionGroup;
using TransportOrder.Messages.Events.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

[TestClass]
public class ActionEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionGroupCreatedForAgv_ValidData_PublishesStartAction()
    {
        var fixture = new ActionEventHandlerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionGroupCreatedForAgv = new ActionGroupCreatedForAgv(actionGroup, Guid.NewGuid());
        await fixture.TestHarness!.Bus.Publish(actionGroupCreatedForAgv);

        (await fixture.ConsumerHarness!.Consumed.Any<ActionGroupCreatedForAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<StartAction>()).ShouldBeTrue();
    }
}