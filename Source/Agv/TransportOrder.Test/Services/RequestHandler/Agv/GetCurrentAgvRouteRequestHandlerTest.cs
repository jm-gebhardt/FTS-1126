using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class GetCurrentAgvRouteRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetAgvRouteRequest_ReturnsAgvRoute()
    {
        var agvCorrelationId = Guid.NewGuid();
        (string[] nodes, Guid[] edges) route = AgvGraphGenerator.CreateRoute();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, route.nodes, route.edges);
        GetCurrentAgvRouteRequestHandlerTestFixture<GetCurrentAgvRoute, GetCurrentAgvRouteRequestHandler> fixture = new GetCurrentAgvRouteRequestHandlerTestFixture<GetCurrentAgvRoute, GetCurrentAgvRouteRequestHandler>()
            .WithAgvRoute(agvCorrelationId, agvRoute)
            .Build();

        Response<GotCurrentAgvRoute> response = await fixture.GetResponseAsync<GotCurrentAgvRoute>(new GetCurrentAgvRoute(agvCorrelationId));

        response.Message.AgvRoute.ShouldBe(agvRoute);
    }

    [TestMethod]
    public async Task Consume_GetAgvRoutesRequest_ReturnsAgvRoutes()
    {
        var agvCorrelationId = Guid.NewGuid();
        (string[] nodes, Guid[] edges) route = AgvGraphGenerator.CreateRoute();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, route.nodes, route.edges);
        Guid[] agvCorrelationIds = { agvCorrelationId };
        
        GetCurrentAgvRouteRequestHandlerTestFixture<GetCurrentAgvRoutes, GetCurrentAgvRouteRequestHandler> fixture = new GetCurrentAgvRouteRequestHandlerTestFixture<GetCurrentAgvRoutes, GetCurrentAgvRouteRequestHandler>()
            .WithAgvRoutes(agvCorrelationIds, [agvRoute])
            .Build();

        Response<GotCurrentAgvRoutes> response = await fixture.GetResponseAsync<GotCurrentAgvRoutes>(new GetCurrentAgvRoutes(agvCorrelationIds));

        response.Message.AgvRoutes.Count.ShouldBe(agvCorrelationIds.Length);
        response.Message.AgvRoutes.First().RemainingNodes.ShouldBe(agvRoute.RemainingNodes);
        response.Message.AgvRoutes.First().RemainingEdges.ShouldBe(agvRoute.RemainingEdges);
        response.Message.AgvRoutes.First().CorrelationId.ShouldBe(agvCorrelationId);
    }
}