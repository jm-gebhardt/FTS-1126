using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class GetCurrentAgvRouteRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetAgvRouteRequest_ReturnsAgvRoute()
    {
        var agvCorrelationId = Guid.NewGuid();
        string[] agvRouteNodes = NodeGenerator.GenerateRouteNodes();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, agvRouteNodes);
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
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, NodeGenerator.GenerateRouteNodes());
        Guid[] agvCorrelationIds = { agvCorrelationId };
        var agvRoutesByAgv = new Dictionary<Guid, CurrentAgvRouteReadModel?>
        {
            { agvCorrelationId, agvRoute }
        };
        GetCurrentAgvRouteRequestHandlerTestFixture<GetCurrentAgvRoutes, GetCurrentAgvRouteRequestHandler> fixture = new GetCurrentAgvRouteRequestHandlerTestFixture<GetCurrentAgvRoutes, GetCurrentAgvRouteRequestHandler>()
            .WithAgvRoutes(agvCorrelationIds, agvRoutesByAgv)
            .Build();

        Response<GotCurrentAgvRoutes> response = await fixture.GetResponseAsync<GotCurrentAgvRoutes>(new GetCurrentAgvRoutes(agvCorrelationIds));

        response.Message.AgvRoutesByCorrelationId.Count.ShouldBe(agvCorrelationIds.Length);
        response.Message.AgvRoutesByCorrelationId.First().Value.ShouldBe(agvRoute);
        response.Message.AgvRoutesByCorrelationId.First().Key.ShouldBe(agvCorrelationId);
    }
}