using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TransportOrder.Domain.Models.ActionGroup;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.Services.Routing;
using TransportOrder.Domain.Validators;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Validators;


public class ActionGroupValidatorTestFixture
{
    private readonly Mock<IRouteService> _routeService = new();

    private readonly Mock<IAgvRegistry> _agvRegistry = new();

    private readonly Mock<IAgvGraphRegistry> _agvGraphRegistry = new();

    public ActionGroupValidator ActionGroupValidator { get; private set; } = null!;

    public ActionGroupValidatorTestFixture()
    {
        var collection = new ServiceCollection()
            .AddTransient(_ => _routeService.Object)
            .AddTransient(_ => _agvRegistry.Object);

        collection.BuildServiceProvider(true);
    }

    public ActionGroupValidatorTestFixture WithAgv(IAgv agv)
    {
        _agvRegistry.Setup(x => x.GetAgv(agv.CorrelationId)).Returns(agv);
        _agvRegistry.Setup(x => x.GetAllAgvs()).Returns(new List<IAgv> { agv }.AsQueryable());
        return this;
    }

    // ReSharper disable once UnusedMember.Global
    public ActionGroupValidatorTestFixture WithAgvs(List<IAgv> agvs)
    {
        _agvRegistry.Setup(x => x.GetAllAgvs()).Returns(agvs.AsQueryable());
        return this;
    }

    public ActionGroupValidatorTestFixture WithRoute(IAgv agv, ActionGroup actionGroup, float? routeCosts = null)
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

    public ActionGroupValidatorTestFixture WithAgvCanHandleRoute(IAgv agv, ActionGroup actionGroup)
    {
        _routeService.Setup(x => x.HasAnyAgvRouteAsync(actionGroup, It.Is<IEnumerable<IAgv>>(list => list.Contains(agv)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return this;
    }

    public ActionGroupValidatorTestFixture Build()
    {
        ActionGroupValidator = new ActionGroupValidator(_agvRegistry.Object, _routeService.Object, new AgvActionValidator(), _agvGraphRegistry.Object);

        return this;
    }
}