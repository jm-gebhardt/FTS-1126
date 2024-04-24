using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class AgvGraphReceivedEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvGraphReceived_RepositoryHasSuccess_PublishesAgvGraphChanged()
    {
        var fixture = new AgvGraphReceivedEventHandlerTestFixture()
            .WithGraphRepositoryAddOrUpdateAgvGraphAsyncReturns(true);

        var agvGraphChanged = await fixture.PublishAndGetFirstMessageAsync<AgvGraphReceived, AgvGraphChanged>(
            fixture.AgvGraphReceived);

        agvGraphChanged.CorrelationId.ShouldBe(fixture.AgvGraphReceived.Graph.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_AgvGraphReceived_RepositoryNoSuccess_PublishesAgvGraphMissing()
    {
        var fixture = new AgvGraphReceivedEventHandlerTestFixture()
            .WithGraphRepositoryAddOrUpdateAgvGraphAsyncReturns(false);

        var graphMissing = await fixture.PublishAndGetFirstMessageAsync<AgvGraphReceived, GraphMissing>(
            fixture.AgvGraphReceived);

        graphMissing.CorrelationId.ShouldBe(fixture.AgvGraphReceived.Graph.CorrelationId);
    }
}