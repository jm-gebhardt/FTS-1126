using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Base;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

public class ChooseAgvActionGroupServiceFixture
{
    [UsedImplicitly]
    public List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> NotStartedActionGroups { get; } = new();

    [UsedImplicitly]
    public Mock<IRouteService> RouteService { get; } = new();

    [UsedImplicitly]
    public Mock<IActionGroupRepository> AgvActionRepository { get; } = new();

    public ShortestRouteChooser ShortestRouteChooser { get; private set; }

    public ChooseAgvActionGroupServiceFixture(bool realRouteService = false)
    {
        AgvActionRepository.Setup(x => x.GetNotStartedActionGroupsAsync(It.IsAny<Guid>())).ReturnsAsync(NotStartedActionGroups);
        ShortestRouteChooser = new ShortestRouteChooser(
            realRouteService ? new RouteService(new Mock<IGraphRepository>().Object, new Mock<IRouteFinder>().Object, new Mock<ILogger<RouteService>>().Object) : RouteService.Object,
            AgvActionRepository.Object,
            new Mock<ILogger<ShortestRouteChooser>>().Object
        );
    }

    public ChooseAgvActionGroupServiceFixture WithAgvHasRouteToDestination(TransportOrder.Domain.Models.Agv.Agv agv, TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup, float? routeCosts = null)
    {
        RouteService.Setup(x => x.GetCostsAsync(agv, It.IsAny<string>(), It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>(), null, It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Route(1f, Array.Empty<string>()));
        return this;
    }

    public ChooseAgvActionGroupServiceFixture WithAgvHasRouteToDestination(TransportOrder.Domain.Models.Agv.Agv agv, string sourceNodeId, string destinationNodeId, TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup, float? routeCosts = null)
    {
        RouteService.Setup(x => x.GetCostsAsync(agv, destinationNodeId, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsAsync(agv, sourceNodeId, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>(), null, It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Route(1f, Array.Empty<string>()));
        return this;
    }

    public ChooseAgvActionGroupServiceFixture AddNotStartedActionGroup(TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        NotStartedActionGroups.Add(actionGroup);
        return this;
    }
}