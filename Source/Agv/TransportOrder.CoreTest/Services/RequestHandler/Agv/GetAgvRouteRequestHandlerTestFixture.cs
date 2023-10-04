using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class GetAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvRouteRetrievalService> _agvRouteService = new();
    public GetAgvRouteRequestHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvRouteService.Object);
        });
    }

    public GetAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvRoute(Guid agvCorrelationId, AgvRouteReadModel agvRouteReadModel, string? lastNodeId = null)
    {
        _agvRouteService.Setup(x => x.GetAgvRouteAsync(agvCorrelationId, lastNodeId)).ReturnsAsync(agvRouteReadModel);
        return this;
    }
    
    public GetAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvRoutes(Guid[] agvCorrelationIds, Dictionary<Guid, AgvRouteReadModel?> agvRoutesByAgv)
    {
        _agvRouteService.Setup(x => x.GetAgvRouteAsync(agvCorrelationIds)).ReturnsAsync(agvRoutesByAgv);
        return this;
    }
    
    public new GetAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}