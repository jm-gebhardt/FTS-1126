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
        queue.Nodes.Add(new QueueNode(efNode.Id, 0, efNode));

        var queueDto = mapper.Map<QueueDto>(queue);

        queueDto.Id.ShouldBe(queue.Id);
        queueDto.Name.ShouldBe(queue.Name);
        queueDto.Nodes.Count.ShouldBe(queue.Nodes.Count);
        queueDto.Nodes.First().NodeId.ShouldBe(queue.Nodes.First().Node!.Name);
        queueDto.Nodes.First().Position.ShouldBe(queue.Nodes.First().Position);
    }

    [TestMethod]
    public void List_Map_ValidData_MapsAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new QueueProfile());

        var nodeListDto = new List<QueueNodeDto> { new("node 1", 0), new("node 2", 1) };

        var nodeList = mapper.Map<List<Common.Messages.Queue.QueueNode>>(nodeListDto);

        nodeList.Count.ShouldBe(nodeListDto.Count);
    }
}