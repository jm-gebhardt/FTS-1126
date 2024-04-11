using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using JetBrains.Annotations;
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

    [PublicAPI]
    public RouteRequestHandlerTestFixture<TRequestClient> WithRoute(TransportOrder.Domain.Models.Agv.Agv agv, string destinationNodeId, string[] nodes)
    {
        _routeServiceMock.WithRoute(0f, nodes);
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