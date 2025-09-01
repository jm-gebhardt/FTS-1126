using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Graph;

[TestClass]
public class GraphCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_UpdateGraphRequest_ShouldPublishGraphUpdatedEvent()
    {
        var fixture = new GraphCommandHandlerTestFixture();

        var graph = new Common.Domain.Models.Graphs.Graph();
        var id = Guid.NewGuid();
        var maps = Mock.Of<List<Common.Domain.Models.Graphs.Map>>();
        await fixture.PublishMessageAsync(new UpdateGraphRequest(id, graph, maps));

        fixture.GraphRepository.Verify(x => x.UpdateGraphAsync(It.IsAny<Common.Domain.Models.Graphs.Graph>()), Times.Once);
        var saveMaps = await fixture.GetFirstPublishedMessageAsync<SaveMaps>();
        saveMaps.Maps.ShouldNotBeNull();
        var graphUpdated = await fixture.GetFirstPublishedMessageAsync<GraphUpdated>();
        graphUpdated.CorrelationId.ShouldBe(id);
        graphUpdated.Graph.ShouldNotBeNull();
        graphUpdated.Maps.ShouldNotBeNull();
    }
}