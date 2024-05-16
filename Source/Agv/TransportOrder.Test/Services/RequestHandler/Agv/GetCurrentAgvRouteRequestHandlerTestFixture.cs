using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

public class GetCurrentAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvRouteRetrievalService> _agvRouteService = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    public GetCurrentAgvRouteRequestHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvRouteService.Object);
            s.AddTransient(_ => _agvDb.Object);
        });
    }

    public GetCurrentAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvRoute(Guid agvCorrelationId, CurrentAgvRouteReadModel currentAgvRouteReadModel, string? lastNodeId = null)
    {
        _agvRouteService.Setup(x => x.GetAgvRouteAsync(agvCorrelationId, lastNodeId)).ReturnsAsync(currentAgvRouteReadModel);
        _agvDb.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { new(agvCorrelationId, Guid.Empty, "") });
        return this;    
    }
    
    public GetCurrentAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvRoutes(Guid[] agvCorrelationIds, ICollection<CurrentAgvRouteReadModel> agvRoutesByAgv)
    {
        _agvRouteService.Setup(x => x.GetAgvRouteAsync(agvCorrelationIds)).ReturnsAsync(agvRoutesByAgv);
        _agvDb.AddAgvs(agvCorrelationIds.Select(c => new TransportOrder.Domain.Models.Agv.Agv(c, Guid.Empty, "")).ToList());
        return this;
    }

    public new GetCurrentAgvRouteRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}