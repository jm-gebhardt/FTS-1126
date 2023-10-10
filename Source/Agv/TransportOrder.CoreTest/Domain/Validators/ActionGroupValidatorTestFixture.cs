using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Validators;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Validators;


public class ActionGroupValidatorTestFixture
{
    private readonly Mock<IRouteService> _routeService = new();

    private readonly Mock<IAgvRepository> _agvRegistry = new();

    private readonly Mock<IAgvGraphRegistry> _agvGraphRegistry = new();

    public ActionGroupValidator ActionGroupValidator { get; private set; } = null!;

    public ActionGroupValidatorTestFixture()
    {
        var collection = new ServiceCollection()
            .AddTransient(_ => _routeService.Object)
            .AddTransient(_ => _agvRegistry.Object);

        collection.BuildServiceProvider(true);
    }

    public ActionGroupValidatorTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvRegistry.Setup(x => x.GetAgvAsync(agv.CorrelationId)).ReturnsAsync(agv);
        _agvRegistry.Setup(x => x.GetAllAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        return this;
    }

    // ReSharper disable once UnusedMember.Global
    public ActionGroupValidatorTestFixture WithAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvRegistry.Setup(x => x.GetAllAgvsAsync()).ReturnsAsync(agvs);
        return this;
    }

    public ActionGroupValidatorTestFixture WithRoute(TransportOrder.Domain.Models.Agv.Agv agv, ActionGroup actionGroup, float? routeCosts = null)
    {
        _routeService.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<CancellationToken>())).ReturnsAsync(routeCosts);
        return this;
    }

    public ActionGroupValidatorTestFixture WithNodesExisting(ActionGroup actionGroup, bool doesNodeExist = true)
    {
        foreach (var action in actionGroup.Actions)
        {
            _agvGraphRegistry.Setup(x => x.NodeExistsAsync(action.TargetNodeId)).ReturnsAsync(doesNodeExist);
        }

        return this;
    }

    public ActionGroupValidatorTestFixture WithAgvCanHandleRoute(TransportOrder.Domain.Models.Agv.Agv agv, ActionGroup actionGroup)
    {
        _routeService.Setup(x => x.HasAnyAgvRouteAsync(actionGroup, It.Is<IEnumerable<TransportOrder.Domain.Models.Agv.Agv>>(list => list.Contains(agv)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return this;
    }

    public ActionGroupValidatorTestFixture Build()
    {
        ActionGroupValidator = new ActionGroupValidator(_agvRegistry.Object, _routeService.Object, new AgvActionValidator(), _agvGraphRegistry.Object);

        return this;
    }
}