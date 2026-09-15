using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.NewGraph;

[TestClass]
public class NewGraphCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_AddNewGraph_SavesTheGraphWithItsResources()
    {
        var fixture = new NewGraphCommandHandlerTestFixture();
        var commandGraph = fixture.AddNewGraphSegment.GraphSegment;

        await fixture.PublishMessageAsync(fixture.AddNewGraphSegment);

        var savedGraph = fixture.SavedGraphSegments.ShouldHaveSingleItem();
        savedGraph.Nodes.Select(n => n.Id).ShouldBe(commandGraph.Nodes.Select(n => n.Id), true);
        savedGraph.Edges.Select(e => e.Id).ShouldBe(commandGraph.Edges.Select(e => e.Id), true);
        savedGraph.Stations.Select(s => s.Id).ShouldBe(commandGraph.Stations.Select(s => s.Id), true);
        savedGraph.Stations.Single().InteractionNodeIds.ShouldHaveSingleItem()
            .ShouldBe(savedGraph.Nodes.Single(n => n.Name == NewGraphGenerator.EndNodeName).Id);
    }

    [TestMethod]
    public async Task Consume_RepositoryThrows_FaultsInsteadOfSwallowing()
    {
        var fixture = new NewGraphCommandHandlerTestFixture().WithFailingRepository();

        await fixture.PublishMessageAsync(fixture.AddNewGraphSegment);

        await fixture.ShouldHavePublishedAsync<Fault<AddNewGraphSegment>>();
    }
}