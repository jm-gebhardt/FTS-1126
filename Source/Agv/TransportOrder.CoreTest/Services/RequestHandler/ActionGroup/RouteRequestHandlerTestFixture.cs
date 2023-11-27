using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.ActionGroup;

public class RouteRequestHandlerTestFixture<TRequestClient> : MediatorTestFixture<TRequestClient, RouteRequestHandler>
    where TRequestClient : class
{
    private readonly Mock<IAgvRepository> _agvRepositoryMock = new();
    private readonly Mock<IRouteService> _routeServiceMock = new();
    private readonly List<TransportOrder.Domain.Models.Agv.Agv> _agvs = new();

    public RouteRequestHandlerTestFixture<TRequestClient> WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvs.Add(agv);

        return this;
    }

    public RouteRequestHandlerTestFixture<TRequestClient> WithRoute(TransportOrder.Domain.Models.Agv.Agv agv, string destinationNodeId, string[] nodes)
    {
        _routeServiceMock.Setup(s => s.GetRouteAsync(agv, destinationNodeId, null, CancellationToken.None)).ReturnsAsync(new Route(0f, nodes));
        return this;
    }

    public override RouteRequestHandlerTestFixture<TRequestClient> Build()
    {
        _agvRepositoryMock.AddAgvsToMockRepository(_agvs);

        AddServices(s => s.AddTransient<IAgvRepository>(_ => _agvRepositoryMock.Object));
        AddServices(s => s.AddTransient<IRouteService>(_ => _routeServiceMock.Object));
        base.Build();

        return this;
    }
}