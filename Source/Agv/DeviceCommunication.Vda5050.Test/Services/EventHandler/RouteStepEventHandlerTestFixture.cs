using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

public class RouteStepEventHandlerTestFixture : MassTransitConsumerTestFixture<RouteStepEventHandler>
{
    private readonly Mock<IRouteExecutionCoordinator> _coordinator = new();
    public readonly Mock<IAgvRepository> AgvRepository = new();

    public RouteStepEventHandlerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddSingleton(_ => _coordinator.Object);
            sc.AddTransient(_ => AgvRepository.Object);
        });
        Build();
    }

    public RouteStepEventHandlerTestFixture AddProgress(Guid orderId, RouteStepProgress? progress)
    {
        _coordinator.Setup(x => x.DetermineRouteStepProgress(orderId, It.IsAny<OrderState>())).ReturnsAsync(progress);
        return this;
    }

    public RouteStepEventHandlerTestFixture AddAgv(Agv agv)
    {
        AgvRepository.Setup(x => x.GetAgvAsync(agv.CorrelationId)).ReturnsAsync(agv);
        return this;
    }
}
