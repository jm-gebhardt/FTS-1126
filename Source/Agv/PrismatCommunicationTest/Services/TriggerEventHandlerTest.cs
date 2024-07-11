using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class TriggerEventHandlerTest
{
    [TestMethod]
    public async Task Consume_TriggerAssignedToNode_ValidData_UpdatesConfiguration()
    {
        var fixture = new HostOrderConsumerTestFixture<TriggerEventHandler>();
        var triggerAssignedToNode = new TriggerAssignedToNode(Guid.NewGuid(), "name", "a node");
        
        await fixture.TestHarness.Bus.Publish(triggerAssignedToNode);
        await fixture.ShouldHaveConsumedAsync<TriggerAssignedToNode>();
        
        fixture.Configuration.TriggerPointMapping.ConcurrentDict[triggerAssignedToNode.TriggerName].ShouldBe(triggerAssignedToNode.NodeId);
    }
}