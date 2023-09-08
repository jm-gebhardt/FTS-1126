using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using Moq;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.Services.ActionGroup;
using TransportOrder.Domain.Services.Routing;
using TransportOrder.Routing;


namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

public class ChooseAgvActionGroupServiceFixture
{
    [UsedImplicitly]
    public List<global::TransportOrder.Domain.Models.ActionGroup.ActionGroup> NotStartedActionGroups { get; } = new();

    [UsedImplicitly]
    public Mock<IRouteService> RouteService { get; } = new();

    [UsedImplicitly]
    public Mock<IActionGroupRegistry> AgvActionRegistry { get; } = new();

    public ShortestRouteChooser ShortestRouteChooser { get; private set; }

    public ChooseAgvActionGroupServiceFixture()
    {
        AgvActionRegistry.Setup(x => x.GetNotStartedActionGroupsAsync(It.IsAny<Guid>())).ReturnsAsync(NotStartedActionGroups);
        ShortestRouteChooser = new ShortestRouteChooser(
            RouteService.Object,
            AgvActionRegistry.Object);
    }

    public ChooseAgvActionGroupServiceFixture WithAgvHasRouteToDestination(IAgv agv, global::TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup, float? routeCosts = null)
    {
        RouteService.Setup(x => x.GetCostsAsync(agv, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new IGraphAlgorithm.Route(1f, Array.Empty<string>()));
        return this;
    }

    public ChooseAgvActionGroupServiceFixture WithAgvHasRouteToDestination(IAgv agv, string sourceNodeId, string destinationNodeId, global::TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup, float? routeCosts = null)
    {
        RouteService.Setup(x => x.GetCostsAsync(agv, destinationNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsAsync(agv, sourceNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new IGraphAlgorithm.Route(1f, Array.Empty<string>()));
        return this;
    }

    public ChooseAgvActionGroupServiceFixture AddNotStartedActionGroup(global::TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        NotStartedActionGroups.Add(actionGroup);
        return this;
    }
}