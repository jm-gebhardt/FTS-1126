using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.ActionGroup;

[TestClass]
public class RouteRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetRoute_RouteExists_RespondsWithRoute()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "S1");
        string[] nodes = { "S", "A", "B", "D" };

        RouteRequestHandlerTestFixture<GetRoute> fixture = new RouteRequestHandlerTestFixture<GetRoute>()
            .WithAgv(agv)
            .WithRoute(agv, "D", nodes)
            .Build();

        Response<GotRoute> response = await fixture.GetResponseAsync<GotRoute>(
            new GetRoute(agv.CorrelationId, "D"));

        var actualRoute = response.Message.Route;
        actualRoute.ShouldNotBeNull();
        actualRoute.CorrelationId.ShouldBe(agv.CorrelationId);
        actualRoute.Nodes.ShouldBe(nodes);
    }

    [TestMethod]
    public async Task Consume_GetRoute_AgvDoesNotExist_ThrowsAgvNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "S1");
        string[] nodes = { "S", "A", "B", "D" };

        RouteRequestHandlerTestFixture<GetRoute> fixture = new RouteRequestHandlerTestFixture<GetRoute>()
            .WithRoute(agv, "D", nodes)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotRoute>(new GetRoute(agv.CorrelationId, "D")));
    }
}