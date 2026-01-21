using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Queue;

[TestClass]
public class QueuesController
{
    [TestMethod]
    public async Task AddQueue_ValidData_ReturnsQueueDto()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue();
        queue.AddNode("a node".ToGuid(), 0);

        var fixture = new QueueControllerTestFixture()
            .WithQueue(queue)
            .Build();

        var nodes = fixture.Mapper.Map<List<QueueNodeDto>>(queue.Nodes);

        var queueDto = await fixture.GetControllerResultAsync(c => c.AddQueue(queue.Name, nodes));
        queueDto.Name.ShouldBe(queue.Name);
        queueDto.Nodes.ShouldBeEquivalentTo(nodes);
    }
    
    [TestMethod]
    public async Task AdjustAgvFillQuantity_ValidData_NoException()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue();
        queue.AddNode("a node".ToGuid(), 0);

        var fixture = new QueueControllerTestFixture()
            .WithQueue(queue)
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.AdjustAgvFillQuantity(queue.Id));
    }
}