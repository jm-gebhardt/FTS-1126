using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Requests;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

public class ActionManagerTestFixture : MassTransitTestFixture<ActionManager>, IActionManagerTestFixtureBuilder
{
    private readonly List<Action<IBusRegistrationConfigurator>> _busRegistrationConfigurators = new();
    private readonly Mock<IAgvRegistry> _mockAgvRegistry;
    private Mock<IActionRegistry>? _pendingActionsRegistry;

    private static string[] Orders => AgvRegistryMock.Orders;

    private static string[] Sources => AgvRegistryMock.Sources;

    private static string[] Destinations => AgvRegistryMock.Destinations;

    private static readonly string[] _leNo = { "111", "222", "333" };

    private static OrderDetails[]? _orderDetails;

    public static OrderDetails[] OrderDetails => _orderDetails ??= GetOrderDetails();

    private static OrderDetails[] GetOrderDetails()
    {
        var orderDetails = new List<OrderDetails>();
        for (int i = 0; i < Orders.Length; i++)
        {
            var details = new OrderDetails(Orders[i].ToGuid(), Orders[i], Sources[i], Destinations[i], _leNo[i], HostOrderCreator.Simulation, DateTime.UtcNow);
            orderDetails.Add(details);
        }

        return orderDetails.ToArray();
    }

    [UsedImplicitly]
    public List<IAgv> Agvs => _mockAgvRegistry.Object.GetAllAgvs().ToList();

    public ActionManager ActionManager { get; private set; } = default!;

    /// <summary>
    /// You must call Build() after constructing an instance!
    /// </summary>
    /// <param name="agvSaga"></param>
    public ActionManagerTestFixture(AgvSaga? agvSaga = null)
    {
        _mockAgvRegistry = AgvRegistryMock.GetAgvRegistry();

        SetActionRegistry(GetActionRegistry());
        SetupBusRegistrationConfigurator(x =>
        {
            x.AddSingleton(new Mock<IOrderRegistry>().Object);
            x.AddSingleton(new Mock<IRoutingPlausibilityChecker>().Object);
            x.AddRequestClient<AssignAgv>();
            x.AddSagaStateMachine<ActionStateMachine, ActionSaga>();
        });

        var mockAgvRegistry = new Mock<IAgvRegistry>();
        mockAgvRegistry.Setup(s => s.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>()));
        mockAgvRegistry.Setup(s => s.Count).Returns(0);

        if (agvSaga != null)
        {
            mockAgvRegistry.Setup(s => s.GetAgv(agvSaga.AgvId.ToGuid()))
                .Returns(agvSaga);
            mockAgvRegistry.Setup(s => s.Count).Returns(1);
        }
    }

    private static Mock<IActionRegistry> GetActionRegistry()
    {
        var registry = new Mock<IActionRegistry>();

        return registry;
    }

    private void SetActionRegistry(Mock<IActionRegistry> actionRegistry)
    {
        _pendingActionsRegistry = actionRegistry;
    }

    public ActionManagerTestFixture PendingActionsRegistryMock(Action<Mock<IActionRegistry>> actionRegistry)
    {
        actionRegistry(_pendingActionsRegistry!);
        return this;
    }

    public IActionManagerTestFixtureBuilder UseAssignAgvConsumer()
    {
        SetupBusRegistrationConfigurator(x =>
        {
            x.AddConsumer<AssignAgvConsumer>();
        });

        return this;
    }

    public IActionManagerTestFixtureBuilder SetActionCreationSucceededEventResponse(ActionCreationSucceededEvent actionCreationSucceededEvent)
    {
        SetupBusRegistrationConfigurator(x =>
        {
            x.AddSingleton(_ => new ConstantRequestHandler<CreateActionRequest, ActionCreationSucceededEvent>(actionCreationSucceededEvent));
            x.AddConsumer<ConstantRequestHandler<CreateActionRequest, ActionCreationSucceededEvent>>();
        });

        return this;
    }

    public IActionManagerTestFixtureBuilder SetAgvAssignmentResponse(AgvAssignmentSucceeded agvAssignmentSucceeded)
    {
        SetupBusRegistrationConfigurator(x =>
        {
            x.AddSingleton(_ => new ConstantRequestHandler<AssignAgv, AgvAssignmentSucceeded>(agvAssignmentSucceeded));
            x.AddConsumer<ConstantRequestHandler<AssignAgv, AgvAssignmentSucceeded>>();
        });

        return this;
    }

    public IActionManagerTestFixtureBuilder SetAgvAssignmentResponse(AgvAssignmentFailed agvAssignmentFailed)
    {
        SetupBusRegistrationConfigurator(x =>
        {
            x.AddSingleton(_ => new ConstantRequestHandler<AssignAgv, AgvAssignmentFailed>(agvAssignmentFailed));
            x.AddConsumer<ConstantRequestHandler<AssignAgv, AgvAssignmentFailed>>();
        });

        return this;
    }

    public ActionManagerTestFixture Build()
    {
        Build(a => _busRegistrationConfigurators.ForEach(b => b(a)), null);

        ActionManager = TestHarness!.Scope.ServiceProvider.GetRequiredService<ActionManager>();
        return this;
    }

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _mockAgvRegistry.Object);
        collection.AddTransient(_ => _pendingActionsRegistry!.Object);
    }

    public IActionManagerTestFixtureBuilder SetupBusRegistrationConfigurator(Action<IBusRegistrationConfigurator> busRegistrationConfigurator)
    {
        _busRegistrationConfigurators.Add(busRegistrationConfigurator);
        return this;
    }

}