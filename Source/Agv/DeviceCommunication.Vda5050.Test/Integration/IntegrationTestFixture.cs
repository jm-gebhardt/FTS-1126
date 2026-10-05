using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Sagas;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.StateMachines;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Integration;

/// <summary>
/// Wires the real <see cref="RouteEventHandler"/>, <see cref="RouteStepEventHandler"/>,
/// <see cref="MqttStateConsumer"/> and <see cref="OrderExecutionStateMachine"/> saga (plus their
/// real, non-database domain service dependencies) into one MassTransit in-memory test harness,
/// mocking only the database boundary: <see cref="IAgvRepository"/>,
/// <see cref="IAgvActionRepository"/>, <see cref="ILifRepository"/>,
/// <see cref="IRouteExecutionRepository"/>.
/// </summary>
public class IntegrationTestFixture : MassTransitTestFixture
{
    private readonly Dictionary<Guid, Agv> _agvs = new();
    private readonly Dictionary<Guid, RouteExecution> _routeExecutions = new();
    private readonly Dictionary<string, List<Layout>> _layoutsByVehicleType = new();

    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IAgvActionRepository> _agvActionRepository = new();
    private readonly Mock<ILifRepository> _lifRepository = new();
    private readonly Mock<IRouteExecutionRepository> _routeExecutionRepository = new();
    private readonly Mock<IFactsheetRequestCacheProvider> _factsheetRequestCache = new();

    public IntegrationTestFixture()
    {
        _agvRepository.Setup(x => x.GetRequiredAgvAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _agvs[id]);
        _agvRepository.Setup(x => x.GetAgvAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _agvs.GetValueOrDefault(id));
        _agvRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>()))
            .Callback((Agv agv, CancellationToken _) => _agvs[agv.CorrelationId] = agv)
            .Returns(Task.CompletedTask);

        _routeExecutionRepository.Setup(x => x.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _routeExecutions.GetValueOrDefault(id));
        _routeExecutionRepository.Setup(x => x.GetRequiredAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _routeExecutions[id]);
        _routeExecutionRepository.Setup(x => x.GetActiveByAgvCorrelationIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid agvId) => _routeExecutions.Values.SingleOrDefault(
                r => r.AgvCorrelationId == agvId && r.LifecycleStatus == RouteExecutionStatus.Active));
        _routeExecutionRepository.Setup(x => x.GetByCurrentOrderIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid orderId) => _routeExecutions.Values.SingleOrDefault(r => r.CurrentOrderId == orderId));
        _routeExecutionRepository.Setup(x => x.AddAsync(It.IsAny<RouteExecution>()))
            .Callback((RouteExecution re) => _routeExecutions[re.RouteId] = re)
            .Returns(Task.CompletedTask);
        _routeExecutionRepository.Setup(x => x.UpdateAsync(It.IsAny<RouteExecution>()))
            .Callback((RouteExecution re) => _routeExecutions[re.RouteId] = re)
            .Returns(Task.CompletedTask);

        _lifRepository.Setup(x => x.GetLayoutsByVehicleTypeIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string vehicleTypeId) =>
                _layoutsByVehicleType.GetValueOrDefault(vehicleTypeId) ?? []);

        _factsheetRequestCache.Setup(x => x.HasAgvRequestedFactsheet(It.IsAny<Guid>())).ReturnsAsync(true);

        AddServices(sc =>
        {
            sc.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            sc.AddTransient(_ => _agvRepository.Object);
            sc.AddTransient(_ => _agvActionRepository.Object);
            sc.AddTransient(_ => _lifRepository.Object);
            sc.AddTransient(_ => _routeExecutionRepository.Object);
            sc.AddTransient(_ => _factsheetRequestCache.Object);

            sc.AddTransient<OrderActionBuilderRegistry>();
            sc.AddTransient<PickOrderActionBuilder>();
            sc.AddTransient<DropOrderActionBuilder>();
            sc.AddTransient<StartChargingOrderActionBuilder>();
            sc.AddTransient<RouteActionMapper>();
            sc.AddTransient<IRouteStepTranslator, RouteStepTranslator>();
            sc.AddTransient<IRouteExecutionCoordinator, RouteExecutionCoordinator>();
            sc.AddSingleton<IOrderActionStatusCacheProvider, OrderActionStatusCache>();
            sc.AddSingleton(typeof(ISequenceCounter<>), typeof(SequenceCounter<>));
        });
        Build();

        // This fixture wires up more real components (2 consumers, a saga, and their full domain
        // service graph) than a typical single-consumer test fixture, so message processing can
        // take noticeably longer under load. The default ITestHarness.TestInactivityTimeout (6s)
        // is tuned for lightweight fixtures and can otherwise elapse and cancel the harness's
        // shared inactivity token mid-test, permanently breaking every later ConsumedMessages/
        // Published check in that test even though the underlying message processing succeeded.
        TestHarness.TestInactivityTimeout = TimeSpan.FromSeconds(20);
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddConsumer<RouteEventHandler>();
        busRegistrationConfigurator.AddConsumer<RouteStepEventHandler>();
        busRegistrationConfigurator.AddMqttConsumer<MqttStateConsumer>();
        busRegistrationConfigurator.AddSagaStateMachine<OrderExecutionStateMachine, OrderExecutionSaga>().InMemoryRepository();
    }

    protected override void ConfigureInMemory(IInMemoryBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        base.ConfigureInMemory(cfg, context);
        MqttTestFixture.ConfigureMqtt(cfg);
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureAutoMapper([
            typeof(Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Profiles.OrderProfile).Assembly,
            typeof(CargoProfile).Assembly
        ]);
    }

    public void AddAgv(Agv agv)
    {
        _agvs[agv.CorrelationId] = agv;
    }

    public void AddLayout(string vehicleTypeId, Layout layout)
    {
        if (!_layoutsByVehicleType.TryGetValue(vehicleTypeId, out var layouts))
        {
            layouts = [];
            _layoutsByVehicleType[vehicleTypeId] = layouts;
        }
        layouts.Add(layout);
    }

    public void AddPickAction(string agvSerialNumber)
    {
        _agvActionRepository
            .Setup(x => x.GetActionsForScopeAsync(agvSerialNumber, ActionScope.Node))
            .ReturnsAsync([
                AgvGenerator.GenerateNodeAction(
                    agvSerialNumber: agvSerialNumber,
                    parameters: [new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = true }])
            ]);
    }

    public RouteExecution GetRouteExecution(Guid routeId) => _routeExecutions[routeId];

    public async Task PublishAndConsumeAsync<T>(T message) where T : class
    {
        await TestHarness.Bus.Publish(message);
        await WaitUntilConsumedAsync<T>();
    }

    public async Task PublishMqttAndConsumeAsync<T>(T message, string routingKey) where T : class
    {
        await TestHarness.Bus.PublishMqtt(message, routingKey, cancellationToken: CancellationToken.None);
        await WaitUntilConsumedAsync<T>();
    }

    // Deliberately polls a plain snapshot (ConsumedMessages.Select<T>()) instead of the
    // cancellation-token-driven ConsumedMessages.Any<T>(): on this fixture's heavier bus (multiple
    // consumers + a saga), Any<T>() was observed to non-deterministically report false for
    // messages that had, per the harness's own debug log, already been received and processed —
    // apparently a race between the async-enumerable wait and the underlying observer. Polling a
    // synchronous snapshot avoids that race.
    // Public so tests can wait for an intermediate domain event (e.g. OrderStateReceived) to be
    // fully processed by the saga before publishing a follow-up message that depends on its effect.
    public async Task WaitUntilConsumedAsync<T>() where T : class
    {
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (ConsumedMessages.Select<T>().Any())
            {
                return;
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"Timed out waiting for {typeof(T).Name} to be consumed.");
    }
}
