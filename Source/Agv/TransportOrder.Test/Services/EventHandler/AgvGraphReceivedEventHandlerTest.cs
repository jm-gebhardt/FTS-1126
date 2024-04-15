using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class AgvGraphReceivedEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvGraphReceived_RepositoryHasSuccess_PublishesAgvGraphChanged()
    {
        var fixture = new AgvGraphReceivedEventHandlerTestFixture()
            .WithGraphRepositoryAddOrUpdateAgvGraphAsyncReturns(true);

        await fixture.PublishMessageAsync(fixture.AgvGraphReceived);

        var agvGraphChanged = await fixture.GetFirstPublishedMessageAsync<AgvGraphChanged>();

        agvGraphChanged.CorrelationId.ShouldBe(fixture.AgvGraphReceived.Graph.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_AgvGraphReceived_RepositoryNoSuccess_PublishesAgvGraphMissing()
    {
        var fixture = new AgvGraphReceivedEventHandlerTestFixture()
            .WithGraphRepositoryAddOrUpdateAgvGraphAsyncReturns(false);

        await fixture.PublishMessageAsync(fixture.AgvGraphReceived);

        var graphMissing = await fixture.GetFirstPublishedMessageAsync<GraphMissing>();

        graphMissing.CorrelationId.ShouldBe(fixture.AgvGraphReceived.Graph.CorrelationId);
    }
}