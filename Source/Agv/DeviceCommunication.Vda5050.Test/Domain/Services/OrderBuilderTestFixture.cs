using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ActionScope = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions.ActionScope;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

public class OrderBuilderTestFixture: FixtureBase
{
    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IAgvGraphRepository> _agvGraphRepository = new();
    private readonly Mock<IAgvActionRepository> _agvActionRepository = new();

    public readonly IOrderBuilder Builder;

    public OrderBuilderTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => new Mock<ILogger<OrderBuilder>>().Object);
            sc.AddTransient(_ => _agvRepository.Object);
            sc.AddTransient(_ => _agvActionRepository.Object);
            sc.AddTransient(_ => _agvGraphRepository.Object);
            sc.AddTransient<IOrderActionBuilder, DetectObjectOrderActionBuilder>();
            sc.AddTransient<IOrderActionBuilder, DropOrderActionBuilder>();
            sc.AddTransient<IOrderActionBuilder, EnableMapOrderActionBuilder>();
            sc.AddTransient<IOrderActionBuilder, FinePositioningOrderActionBuilder>();
            sc.AddTransient<IOrderActionBuilder, InitPositionOrderActionBuilder>();
            sc.AddTransient<IOrderActionBuilder, PickOrderActionBuilder>();
            sc.AddTransient<IOrderActionBuilder, StartChargingOrderActionBuilder>();
            sc.AddTransient<IOrderActionBuilder, StopChargingOrderActionBuilder>();
            sc.AddTransient<IOrderActionBuilder, WaitForTriggerOrderActionBuilder>();
            sc.AddTransient<IOrderBuilder, OrderBuilder>();

            sc.AddTransient<OrderActionBuilderRegistry>();
        });
        base.Build();
        Builder = Provider.GetService<IOrderBuilder>()!;
    }

    public OrderBuilderTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    public OrderBuilderTestFixture AgvAgvGraph(AgvGraph agvGraph)
    {
        _agvGraphRepository.Setup(x => x.GetAgvGraphAsync(It.IsAny<Guid>())).ReturnsAsync(agvGraph);
        return this;
    }

    public OrderBuilderTestFixture AddActions(ICollection<AgvAction> actions)
    {
        _agvActionRepository.Setup(x => x.GetActionsForScopeAsync(It.IsAny<string>(), ActionScope.Node)).ReturnsAsync(actions);
        _agvActionRepository.Setup(x => x.GetActionsForScopeAsync(It.IsAny<string>(), ActionScope.Edge)).ReturnsAsync([]);
        return this;
    }
}