using AutoFixture;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Profiles;

[TestClass]
public class QueueProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereQueueIsMappedToMessagesQueue()
    {
        var mapper = MappingHelper.GetMapper(new QueueProfile());
        var fixture = new Fixture();
        var queue = fixture
            .Build<Queue>()
            .With(q => q.Nodes, fixture
                .Build<QueueNode>()
                .With(n => n.Node, fixture
                    .Build<GraphNode>()
                    .With(n => n.Id)
                    .With(n => n.Name)
                    .OmitAutoProperties()
                    .Create())
                .CreateMany(1).ToList())
            .Create();

        foreach (var node in queue.Nodes)
        {
            node.Occupy(Guid.NewGuid());
            node.Reserve(Guid.NewGuid());
        }

        var queueMessage = mapper.Map<Common.Messages.Queue.Queue>(queue);
        queueMessage.CorrelationId.ShouldBe(queue.Id);
        queueMessage.Name.ShouldBe(queue.Name);
        queueMessage.QueueNodes.Count.ShouldBe(queue.Nodes.Count);
        for (int i = 0; i < queue.Nodes.Count; i++)
        {
            var expectedNode = queue.Nodes.Single(n => n.Node!.Id == queueMessage.QueueNodes[i].Node.Id);
            var actualNode = queueMessage.QueueNodes[i];
            actualNode.Node.Id.ShouldBe(expectedNode.Node!.Id);
            actualNode.Node.Name.ShouldBe(expectedNode.Node.Name);
            actualNode.Position.ShouldBe(expectedNode.Position);
            actualNode.ReservedAgv.ShouldBe(expectedNode.ReservedAgv);
            actualNode.OccupiedAgv.ShouldBe(expectedNode.OccupiedAgv);
        }
    }
}