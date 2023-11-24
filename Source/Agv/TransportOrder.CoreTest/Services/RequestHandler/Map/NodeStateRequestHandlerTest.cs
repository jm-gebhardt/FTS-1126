using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Map;

[TestClass]
public class NodeStateRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetMapNodeStates_ValidData_RespondsGotMapNodeStates()
    {
        var nodeStates = new List<NodeLock> { new("nodeId", NodeLockState.Free, false, null, DateTime.UtcNow) };
        NodeStateRequestHandlerTestFixture<GetMapNodeStates, NodeStateRequestHandler> fixture = new NodeStateRequestHandlerTestFixture<GetMapNodeStates, NodeStateRequestHandler>()
            .WithNodeStates(nodeStates)
            .Build();

        Response<GotMapNodeStates> response = await fixture.GetResponseAsync<GotMapNodeStates>(new GetMapNodeStates(Guid.NewGuid()));
        response.Message.NodeLocks.ShouldBe(nodeStates, ignoreOrder:true);
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