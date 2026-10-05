using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class GetCurrentAgvRouteRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetAgvRouteRequest_ReturnsAgvRoute()
    {
        var agvCorrelationId = Guid.NewGuid();
        var route = AgvGraphGenerator.CreateRoute();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, route.nodes, route.edges);
        var fixture = new GetCurrentAgvRouteRequestHandlerTestFixture<GetCurrentAgvRoute, GetCurrentAgvRouteRequestHandler>()
            .WithAgvRoute(agvCorrelationId, agvRoute)
            .Build();

        var response = await fixture.GetResponseAsync<GotCurrentAgvRoute>(new GetCurrentAgvRoute(agvCorrelationId));

        response.Message.AgvRoute.ShouldBe(agvRoute);
    }

    [TestMethod]
    public async Task Consume_GetAgvRoutesRequest_ReturnsAgvRoutes()
    {
        var agvCorrelationId = Guid.NewGuid();
        var route = AgvGraphGenerator.CreateRoute();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, route.nodes, route.edges);
        Guid[] agvCorrelationIds = { agvCorrelationId };

        var fixture = new GetCurrentAgvRouteRequestHandlerTestFixture<GetCurrentAgvRoutes, GetCurrentAgvRouteRequestHandler>()
            .WithAgvRoutes(agvCorrelationIds, [agvRoute])
            .Build();

        var response = await fixture.GetResponseAsync<GotCurrentAgvRoutes>(new GetCurrentAgvRoutes(agvCorrelationIds));

        response.Message.AgvRoutes.Count.ShouldBe(agvCorrelationIds.Length);
        response.Message.AgvRoutes.First().RemainingNodes.ShouldBe(agvRoute.RemainingNodes);
        response.Message.AgvRoutes.First().RemainingEdges.ShouldBe(agvRoute.RemainingEdges);
        response.Message.AgvRoutes.First().CorrelationId.ShouldBe(agvCorrelationId);
    }
}