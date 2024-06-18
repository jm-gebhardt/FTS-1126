using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.ActionGroup;

public class RouteRequestHandlerTestFixture<TRequestClient> : MediatorTestFixture<TRequestClient, RouteRequestHandler, MockConsumer>
    where TRequestClient : class
{
    private readonly Mock<IRouteService> _routeServiceMock = new();
    private readonly List<TransportOrder.Domain.Models.Agv.Agv> _agvs = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    public RouteRequestHandlerTestFixture<TRequestClient> WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvs.Add(agv);
        _agvDb.AddAgvData((AgvData?)null);
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
        _agvDb.AddAgvs(_agvs);
        base.Build();

        return this;
    }
}