using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
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

    public IRouteService RouteService { get; } = new RouteService();

    public Mock<IActionGroupRegistry> AgvActionRegistry { get; } = new();

    public ShortestRouteChooser ShortestRouteChooser { get; private set; }

    public ChooseAgvActionGroupServiceFixture()
    {
        AgvActionRegistry.Setup(x => x.GetNotStartedActionGroupsAsync()).ReturnsAsync(NotStartedActionGroups);
        ShortestRouteChooser = new ShortestRouteChooser(
            RouteService,
            AgvActionRegistry.Object,
            Logger.Object);
    }

    public ChooseAgvActionGroupServiceFixture AddNotStartedActionGroup(ActionGroup actionGroup)
    {
        NotStartedActionGroups.Add(actionGroup);
        return this;
    }
}