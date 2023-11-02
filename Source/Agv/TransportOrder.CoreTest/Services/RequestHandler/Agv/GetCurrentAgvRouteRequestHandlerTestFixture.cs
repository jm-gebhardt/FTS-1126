using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class GetCurrentAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvRouteRetrievalService> _agvRouteService = new();

    public GetCurrentAgvRouteRequestHandlerTestFixture()
    {
        AddServices(s => { s.AddTransient(_ => _agvRouteService.Object); });
    }

    public GetCurrentAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvRoute(Guid agvCorrelationId, CurrentAgvRouteReadModel currentAgvRouteReadModel, string? lastNodeId = null)
    {
        _agvRouteService.Setup(x => x.GetAgvRouteAsync(agvCorrelationId, lastNodeId)).ReturnsAsync(currentAgvRouteReadModel);
        return this;
    }

    public GetCurrentAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvRoutes(Guid[] agvCorrelationIds, Dictionary<Guid, CurrentAgvRouteReadModel?> agvRoutesByAgv)
    {
        _agvRouteService.Setup(x => x.GetAgvRouteAsync(agvCorrelationIds)).ReturnsAsync(agvRoutesByAgv);
        return this;
    }

    public new GetCurrentAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}