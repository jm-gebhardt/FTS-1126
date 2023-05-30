using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.Extensions.Logging;
using Moq;
// ReSharper disable MemberCanBePrivate.Global

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions.Choosers;

public class ChooseAgvActionGroupServiceFixture
{
    private Mock<ILogger<ShortestRouteChooser>> Logger { get; } = new();

    public List<ActionGroup> NotStartedActionGroups { get; } = new();

    public Mock<IRouteService> RouteService { get; } = new();

    public Mock<IActionGroupRegistry> AgvActionRegistry { get; } = new();

    public ShortestRouteChooser ShortestRouteChooser { get; private set; }

    public ChooseAgvActionGroupServiceFixture()
    {
        AgvActionRegistry.Setup(x => x.GetNotStartedActionGroupsAsync()).ReturnsAsync(NotStartedActionGroups);
        ShortestRouteChooser = new ShortestRouteChooser(
            RouteService.Object,
            AgvActionRegistry.Object,
            Logger.Object);
    }

    public ChooseAgvActionGroupServiceFixture WithAgvHasRouteToDestination(IAgv agv, bool hasRoute = true, float? routeCosts = null)
    {
        RouteService.Setup(x => x.HasRoute(agv, It.IsAny<string>(), out routeCosts)).Returns(hasRoute);
        RouteService.Setup(x => x.GetRoute(agv, It.IsAny<string>())).Returns(new IGraphAlgorithm.Route(1f, Array.Empty<string>()));
        return this;
    }

    public ChooseAgvActionGroupServiceFixture AddNotStartedActionGroup(ActionGroup actionGroup)
    {
        NotStartedActionGroups.Add(actionGroup);
        return this;
    }
}