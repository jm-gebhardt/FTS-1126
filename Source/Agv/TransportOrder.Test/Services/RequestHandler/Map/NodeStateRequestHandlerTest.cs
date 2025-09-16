using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

[TestClass]
public class NodeStateRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetMapNodeStates_ValidData_RespondsGotMapNodeStates()
    {
        var nodeStates = new List<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser)>
        {
            (new NodeLock("nodeId".ToGraphNode(), NodeLockState.Free, false, null, DateTime.UtcNow), Guid.NewGuid(), null, null, null)
        };

        NodeStateRequestHandlerTestFixture<GetMapNodeStates, NodeStateRequestHandler> fixture = new NodeStateRequestHandlerTestFixture<GetMapNodeStates, NodeStateRequestHandler>()
            .WithEmptyRepository()
            .WithNodeStates(nodeStates)
            .Build();

        Response<GotMapNodeStates> response = await fixture.GetResponseAsync<GotMapNodeStates>(new GetMapNodeStates(Guid.NewGuid()));
        response.Message.NodeStates.ShouldBe(nodeStates, ignoreOrder: true);
    }

    [TestMethod]
    public async Task Consume_GetMapNodeStates_UnknownMap_ThrowsMapNotFoundException()
    {
        NodeStateRequestHandlerTestFixture<GetMapNodeStates, NodeStateRequestHandler> fixture = new NodeStateRequestHandlerTestFixture<GetMapNodeStates, NodeStateRequestHandler>()
            .WithEmptyRepository()
            .Build();
        await MediatorShould.ThrowRequestExceptionAsync<MapNotFoundException>(async () => await fixture.GetResponseAsync<GotMapNodeStates>(new GetMapNodeStates(Guid.NewGuid())));
    }
}