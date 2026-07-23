using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

public class RouteEventHandlerTestFixture : MassTransitConsumerTestFixture<RouteEventHandler>
{
    private readonly Mock<IRouteExecutionCoordinator> _coordinator = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public RouteEventHandlerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddSingleton(_ => _coordinator.Object);
            sc.AddTransient(_ => _agvRepository.Object);
        });
        Build();
    }

    public RouteEventHandlerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetRequiredAgvAsync(agv.CorrelationId)).ReturnsAsync(agv);
        return this;
    }

    public RouteEventHandlerTestFixture AddResult(RouteCoordinationResult result)
    {
        _coordinator.Setup(x => x.PlanRouteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RouteStep[]>())).ReturnsAsync(result);
        _coordinator.Setup(x => x.UpdateRouteAsync(It.IsAny<Guid>(), It.IsAny<RouteStep[]>())).ReturnsAsync(result);
        _coordinator.Setup(x => x.ReleaseRouteStepsAsync(It.IsAny<Guid>(), It.IsAny<Guid[]>())).ReturnsAsync(result);
        _coordinator.Setup(x => x.AbortRouteAsync(It.IsAny<Guid>())).ReturnsAsync(result);
        return this;
    }

    public void VerifyAgvUpdated()
    {
        _agvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
