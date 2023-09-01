using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Moq;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.Services.ActionGroup;
using TransportOrder.Domain.Services.Routing;
using TransportOrder.Routing;

// ReSharper disable MemberCanBePrivate.Global

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

public class ChooseAgvActionGroupServiceFixture
{
    public List<Common.Messages.Actions.ActionGroup> NotStartedActionGroups { get; } = new();

    public Mock<IRouteService> RouteService { get; } = new();

    public Mock<IActionGroupRegistry> AgvActionRegistry { get; } = new();

    public ShortestRouteChooser ShortestRouteChooser { get; private set; }

    public ChooseAgvActionGroupServiceFixture()
    {
        AgvActionRegistry.Setup(x => x.GetNotStartedActionGroupsAsync(It.IsAny<Guid>())).ReturnsAsync(NotStartedActionGroups);
        ShortestRouteChooser = new ShortestRouteChooser(
            RouteService.Object,
            AgvActionRegistry.Object);
    }

    public ChooseAgvActionGroupServiceFixture WithAgvHasRouteToDestination(IAgv agv, bool hasRoute = true, float? routeCosts = null)
    {
        RouteService.Setup(x => x.HasRoute(agv, It.IsAny<string>(), out routeCosts)).Returns(hasRoute);
        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>())).ReturnsAsync(new IGraphAlgorithm.Route(1f, Array.Empty<string>()));
        return this;
    }
    
    public ChooseAgvActionGroupServiceFixture WithAgvHasRouteToDestination(IAgv agv, string sourceNodeId, string destinationNodeId, bool hasRoute = true, float? routeCosts = null)
    {
        RouteService.Setup(x => x.HasRoute(agv, destinationNodeId, out routeCosts)).Returns(hasRoute);
        RouteService.Setup(x => x.HasRoute(agv, sourceNodeId, out routeCosts)).Returns(hasRoute);
        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>())).ReturnsAsync(new IGraphAlgorithm.Route(1f, Array.Empty<string>()));
        return this;
    }

    public ChooseAgvActionGroupServiceFixture AddNotStartedActionGroup(Common.Messages.Actions.ActionGroup actionGroup)
    {
        NotStartedActionGroups.Add(actionGroup);
        return this;
    }
}