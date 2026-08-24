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
        var commandGraph = fixture.AddNewGraph.Graph;

        await fixture.PublishMessageAsync(fixture.AddNewGraph);

        var savedGraph = fixture.SavedGraphs.ShouldHaveSingleItem();
        savedGraph.Nodes.Select(n => n.Id).ShouldBe(commandGraph.Nodes.Select(n => n.Id), true);
        savedGraph.Edges.Select(e => e.Id).ShouldBe(commandGraph.Edges.Select(e => e.Id), true);
        savedGraph.Stations.Select(s => s.Id).ShouldBe(commandGraph.Stations.Select(s => s.Id), true);
        savedGraph.Stations.Single().InteractionNodes.ShouldHaveSingleItem()
            .ShouldBeSameAs(savedGraph.Nodes.Single(n => n.Name == NewGraphGenerator.EndNodeName));
    }

    [TestMethod]
    public async Task Consume_RepositoryThrows_FaultsInsteadOfSwallowing()
    {
        var fixture = new NewGraphCommandHandlerTestFixture().WithFailingRepository();

        await fixture.PublishMessageAsync(fixture.AddNewGraph);

        await fixture.ShouldHavePublishedAsync<Fault<AddNewGraph>>();
    }
}