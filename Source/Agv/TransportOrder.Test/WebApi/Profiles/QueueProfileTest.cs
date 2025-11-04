using AutoFixture;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class QueueProfileTest
{
    private readonly Fixture _fixture = new();

    [TestMethod]
    public void Map_ValidData_MapsAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new QueueProfile());
        var queue = new Queue("a queue");
        const string nodeName = "node name";
        var efNode = new EfNode
        {
            Id = nodeName.ToGuid(),
            Name = nodeName
        };
        queue.Nodes.Add(new QueueNode(efNode.Id, 0) { Node = efNode });

        var queueDto = mapper.Map<QueueDto>(queue);

        queueDto.Id.ShouldBe(queue.Id);
        queueDto.Name.ShouldBe(queue.Name);
        queueDto.Nodes.Count.ShouldBe(queue.Nodes.Count);
        queueDto.Nodes.First().NodeId.ShouldBe(queue.Nodes.First().Node!.Name);
        queueDto.Nodes.First().Position.ShouldBe(queue.Nodes.First().Position);
    }

    [TestMethod]
    public void Map_ReservedAgvSet_MapsReservedAgv()
    {
        var mapper = MappingHelper.GetMapper(new QueueProfile());
        var queue = new Queue(null!);
        var queueNode = new QueueNode(Guid.NewGuid(), 0);
        var agvCorrelationId = Guid.NewGuid();
        queueNode.Reserve(agvCorrelationId);
        queue.Nodes.Add(queueNode);

        var queueDto = mapper.Map<QueueDto>(queue);

        queueDto.Nodes.Single().ReservedAgv.ShouldBe(agvCorrelationId);
        queueDto.Nodes.Single().OccupiedAgv.ShouldBeNull();
    }

    [TestMethod]
    public void Map_OccupiedAgvSet_MapsOccupiedAgv()
    {
        var mapper = MappingHelper.GetMapper(new QueueProfile());
        var queue = new Queue(null!);
        var queueNode = new QueueNode(Guid.NewGuid(), 0);
        var agvCorrelationId = Guid.NewGuid();
        queueNode.Occupy(agvCorrelationId);
        queue.Nodes.Add(queueNode);

        var queueDto = mapper.Map<QueueDto>(queue);

        queueDto.Nodes.Single().OccupiedAgv.ShouldBe(agvCorrelationId);
        queueDto.Nodes.Single().ReservedAgv.ShouldBeNull();
    }

    [TestMethod]
    public void List_Map_ValidData_MapsAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new QueueProfile());

        List<QueueNodeDto> nodeListDto = _fixture.CreateMany<QueueNodeDto>(2).ToList();

        var nodeList = mapper.Map<List<Common.Messages.Queue.QueueNode>>(nodeListDto);
        nodeList.Count.ShouldBe(nodeListDto.Count);
    }

    [TestMethod]
    public void List_Map_MessageQueue_MapsAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new QueueProfile());

        var queue = _fixture.Build<Common.Messages.Queue.Queue>()
            .With(q => q.QueueNodes, _fixture.CreateMany<Common.Messages.Queue.QueueNode>(2).ToList)
            .Create();

        var dto = mapper.Map<QueueDto>(queue);
        dto.Id.ShouldBe(queue.CorrelationId);
        dto.Name.ShouldBe(queue.Name);
        dto.Nodes.Count.ShouldBe(queue.QueueNodes.Count);
        for (int i = 0; i < queue.QueueNodes.Count; i++)
        {
            var queueNodeDto = dto.Nodes.Skip(i).First();
            var queueNode = queue.QueueNodes.Single(qn => qn.Node.Name == queueNodeDto.NodeId);
            queueNodeDto.Position.ShouldBe(queueNode.Position);
            queueNodeDto.ReservedAgv.ShouldBe(queueNode.ReservedAgv);
            queueNodeDto.OccupiedAgv.ShouldBe(queueNode.OccupiedAgv);
        }
    }
}