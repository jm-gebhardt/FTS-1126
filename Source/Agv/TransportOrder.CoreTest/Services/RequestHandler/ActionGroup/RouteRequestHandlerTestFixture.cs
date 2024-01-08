using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.ActionGroup;

public class RouteRequestHandlerTestFixture<TRequestClient> : MediatorTestFixture<TRequestClient, RouteRequestHandler>
    where TRequestClient : class
{
    private readonly Mock<IRouteService> _routeServiceMock = new();
    private readonly List<TransportOrder.Domain.Models.Agv.Agv> _agvs = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    public RouteRequestHandlerTestFixture<TRequestClient> WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvs.Add(agv);
        _agvDb.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync((AgvData?)null);
        return this;
    }

    public RouteRequestHandlerTestFixture<TRequestClient> WithRoute(TransportOrder.Domain.Models.Agv.Agv agv, string destinationNodeId, string[] nodes)
    {
        _routeServiceMock.Setup(s => s.GetRouteAsync(agv, destinationNodeId, null, It.IsAny<AgvData>(), CancellationToken.None)).ReturnsAsync(new Route(0f, nodes));
        return this;
    }

    public override RouteRequestHandlerTestFixture<TRequestClient> Build()
    {
        AddServices(s => s.AddTransient(_ => _agvDb.Object));
        AddServices(s => s.AddTransient(_ => _routeServiceMock.Object));
        _agvDb.AddAgvsToMockRepository(_agvs);
        base.Build();

        return this;
    }
}