using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvRouteRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetAgvRouteRequest_ReturnsAgvRoute()
    {
        var agvCorrelationId = Guid.NewGuid();
        string[] agvRouteNodes = NodeGenerator.GenerateRouteNodes();
        var agvRoute = new AgvRouteReadModel(agvRouteNodes);
        GetAgvRouteRequestHandlerTestFixture<GetAgvRoute, GetAgvRouteRequestHandler> fixture = new GetAgvRouteRequestHandlerTestFixture<GetAgvRoute, GetAgvRouteRequestHandler>()
            .WithAgvRoute(agvCorrelationId, agvRoute)
            .Build();

        Response<GotAgvRoute> response = await fixture.GetResponseAsync<GotAgvRoute>(new GetAgvRoute(agvCorrelationId));

        response.Message.AgvRoute.ShouldBe(agvRoute);
    }

    [TestMethod]
    public async Task Consume_GetAgvRoutesRequest_ReturnsAgvRoutes()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new AgvRouteReadModel(NodeGenerator.GenerateRouteNodes());
        Guid[] agvCorrelationIds = { agvCorrelationId };
        var agvRoutesByAgv = new Dictionary<Guid, AgvRouteReadModel?>
        {
            { agvCorrelationId, agvRoute }
        };
        GetAgvRouteRequestHandlerTestFixture<GetAgvRoutes, GetAgvRouteRequestHandler> fixture = new GetAgvRouteRequestHandlerTestFixture<GetAgvRoutes, GetAgvRouteRequestHandler>()
            .WithAgvRoutes(agvCorrelationIds, agvRoutesByAgv)
            .Build();
       
        Response<GotAgvRoutes> response = await fixture.GetResponseAsync<GotAgvRoutes>(new GetAgvRoutes(agvCorrelationIds));

        response.Message.AgvRoutesByCorrelationId.Count.ShouldBe(agvCorrelationIds.Length);
        response.Message.AgvRoutesByCorrelationId.First().Value.ShouldBe(agvRoute);
        response.Message.AgvRoutesByCorrelationId.First().Key.ShouldBe(agvCorrelationId);
    }
}