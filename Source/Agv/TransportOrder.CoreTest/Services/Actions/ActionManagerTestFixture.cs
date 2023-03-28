using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

public class ActionManagerTestFixture : MassTransitTestFixture<ActionManager>, IActionManagerTestFixtureBuilder
{
    private readonly List<Action<IBusRegistrationConfigurator>> _busRegistrationConfigurators = new();

    private readonly Mock<IAgvRegistry> _mockAgvRegistry;
    private IDbContextFactory<TransportOrderDbContext>? _transportOrderContextFactory;

    private static OrderDetails[]? _orderDetails;

    private static OrderDetails[] GetOrderDetails()
    {
        var orderDetails = new List<OrderDetails>();
        for (int i = 0; i < AgvRegistryMock.Orders.Length; i++)
        {
            var details = new OrderDetails(
                AgvRegistryMock.Orders[i].ToGuid(),
                AgvRegistryMock.Orders[i],
                AgvRegistryMock.Sources[i],
                AgvRegistryMock.Destinations[i],
                AgvRegistryMock.LoadUnits[i],
                HostOrderCreator.Simulation,
                DateTime.UtcNow);

            orderDetails.Add(details);
        }

        return orderDetails.ToArray();
    }

    // ReSharper disable all
    public static OrderDetails[] OrderDetails => _orderDetails ??= GetOrderDetails();
    public List<IAgv> Agvs => _mockAgvRegistry.Object.GetAllAgvs().ToList();
    public IOrderActionRegistry CreateOrderActionRegistry() => new EfOrderActionRegistry(_transportOrderContextFactory!);
    public IAgvActionRegistry AgvActionRegistry { get; private set; } = default!;
    public ActionManager ActionManager { get; private set; } = default!;
    // ReSharper enable all

    /// <summary>
    /// Test fixture using mocked AGV registry and in-memory-database for actions.
    /// </summary>
    /// <remarks>
    /// You must call <see cref="Build"/>> after constructing an instance!
    /// </remarks>
    /// <param name="agvSaga"></param>
    public ActionManagerTestFixture(AgvSaga? agvSaga = null)
    {
        _mockAgvRegistry = AgvRegistryMock.GetAgvRegistry();

        SetupBusRegistrationConfigurator(x =>
        {
            x.AddDbContextFactory<TransportOrderDbContext>(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            });
            x.AddDbContextFactory<SagaPersistenceDbContext>(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            });
            x.AddSingleton<IAgvActionRegistry, EfAgvActionRegistry>();
            x.AddRequestClient<AssignAgv>();
        });

        var mockAgvRegistry = new Mock<IAgvRegistry>();
        mockAgvRegistry.Setup(s => s.GetReadyAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>()));
        mockAgvRegistry.Setup(s => s.Count).Returns(0);

        if (agvSaga != null)
        {
            mockAgvRegistry.Setup(s => s.GetAgv(agvSaga.AgvId.ToGuid()))
                .Returns(agvSaga);
            mockAgvRegistry.Setup(s => s.Count).Returns(1);
        }
    }

    public ActionManagerTestFixture Build()
    {
        Build(a => _busRegistrationConfigurators.ForEach(b => b(a)), null);

        _transportOrderContextFactory = TestHarness!.Scope.ServiceProvider.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();
        ActionManager = TestHarness!.Scope.ServiceProvider.GetRequiredService<ActionManager>();
        AgvActionRegistry = TestHarness!.Scope.ServiceProvider.GetRequiredService<IAgvActionRegistry>();

        return this;
    }

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _mockAgvRegistry.Object);
    }

    public IActionManagerTestFixtureBuilder UseAssignAgvConsumer()
    {
        SetupBusRegistrationConfigurator(x =>
        {
            x.AddConsumer<AssignAgvConsumer>();
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

    public IActionManagerTestFixtureBuilder SetupBusRegistrationConfigurator(Action<IBusRegistrationConfigurator> busRegistrationConfigurator)
    {
        _busRegistrationConfigurators.Add(busRegistrationConfigurator);
        return this;
    }
}