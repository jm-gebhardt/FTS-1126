using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Validators;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Validators;

public class ActionGroupValidatorTestFixture : FixtureBase
{
    private readonly Mock<IRouteService> _routeService = new();

    private readonly Mock<IAgvRepository> _agvRepository = new();

    private readonly Mock<IGraphRepository> _agvGraphRepository = new();

    public ActionGroupValidator ActionGroupValidator { get; private set; } = null!;

    public ActionGroupValidatorTestFixture()
    {
        AddServices(s =>
            s.AddTransient(_ => _routeService.Object)
                .AddTransient(_ => _agvRepository.Object)
        );

        Build();
    }

    public ActionGroupValidatorTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvRepository.Setup(x => x.GetAgvAsync(agv.CorrelationId)).ReturnsAsync(agv);
        _agvRepository.Setup(x => x.GetAllAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        return this;
    }

    public ActionGroupValidatorTestFixture WithRoute(TransportOrder.Domain.Models.Agv.Agv agv, ActionGroup actionGroup, float? routeCosts = null)
    {
        _routeService.WithRouteCostsFirstAction(agv, actionGroup, routeCosts);
        return this;
    }

    public ActionGroupValidatorTestFixture WithNodesExisting(ActionGroup actionGroup, bool doesNodeExist = true)
    {
        foreach (var action in actionGroup.Actions)
        {
            _agvGraphRepository.Setup(x => x.NodeExistsAsync(action.TargetNodeId)).ReturnsAsync(doesNodeExist);
        }

        return this;
    }

    public ActionGroupValidatorTestFixture WithAgvCanHandleRoute(TransportOrder.Domain.Models.Agv.Agv agv, ActionGroup actionGroup)
    {
        _routeService.Setup(x => x.HasAnyAgvRouteAsync(actionGroup, It.Is<IEnumerable<TransportOrder.Domain.Models.Agv.Agv>>(list => list.Contains(agv)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return this;
    }

    public new ActionGroupValidatorTestFixture Build()
    {
        ActionGroupValidator = new ActionGroupValidator(_agvRepository.Object, _routeService.Object, new AgvActionValidator(), _agvGraphRepository.Object);

        return this;
    }
}