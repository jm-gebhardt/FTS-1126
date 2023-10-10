using System.Diagnostics;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Provides a test fixture for mass transit without any consumers.
/// </summary>
[UsedImplicitly]
public class MassTransitTestFixture
{
    private readonly Action<IServiceCollection>? _services;
    private readonly Action<IBusRegistrationConfigurator>? _busRegistrationConfigurator;
    private readonly Action<ITestHarness>? _busHarnessConfigurator;
    private readonly bool _useReducedTestInactivityTimeout;
    private Lazy<ITestHarness> _testHarness = null!;

    /// <summary>
    /// Initializes a new <see cref="MassTransitTestFixture" /> instance.
    /// </summary>
    /// <param name="services">A service collection action used to provide additional registrations.</param>
    /// <param name="busRegistrationConfigurator">A configurator action for additional bus configurations.</param>
    /// <param name="busHarnessConfigurator">A configurator action for the <see cref="ITestHarness" />.</param>
    /// <param name="useReducedTestInactivityTimeout">
    /// A value indicating whether the test inactivity timeout should be reduced.
    /// This should be set to 'true' for tests asserting that a specific message has NOT been published.
    /// </param>
    /// <param name="start">A value indicating whether to start this instance immediately by building the service provider.</param>
    public MassTransitTestFixture(Action<IServiceCollection>? services = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<ITestHarness>? busHarnessConfigurator = null,
        bool useReducedTestInactivityTimeout = false,
        bool start = true)
    {
        _services = services;
        _busRegistrationConfigurator = busRegistrationConfigurator;
        _busHarnessConfigurator = busHarnessConfigurator;
        _useReducedTestInactivityTimeout = useReducedTestInactivityTimeout;

        if (!start)
        {
            return;
        }
        Init();
        Start();
    }

    private void Init(IServiceCollection? serviceCollection = null)
    {
        ServiceCollection = serviceCollection ?? new ServiceCollection();
        ServiceCollection
            .AddLogging()
            .AddMassTransitTestHarness(x =>
            {
                _busRegistrationConfigurator?.Invoke(x);
                x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
            });
        _services?.Invoke(ServiceCollection);
    }

    private void Start(IServiceProvider? serviceProvider = null)
    {
        Provider = serviceProvider ?? ServiceCollection!.BuildServiceProvider(true);
        const int defaultReducedTestInactivityTimeoutInMs = 200;
        _testHarness = new Lazy<ITestHarness>(() =>
        {
            var harness = Provider.GetTestHarness();
            harness.TestInactivityTimeout = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(3);
            if (_useReducedTestInactivityTimeout)
            {
                harness.TestInactivityTimeout = TimeSpan.FromMilliseconds(defaultReducedTestInactivityTimeoutInMs);
            }

            _busHarnessConfigurator?.Invoke(harness);
            return harness;
        });
      
    }

    private IServiceCollection? ServiceCollection { get; set; }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    private IServiceProvider? Provider { get; set; }

    /// <summary>
    /// Gets the test harness.
    /// </summary>
    [UsedImplicitly]
    public ITestHarness TestHarness
    {
        get
        {
            if (!_testHarness.IsValueCreated)
            {
                _testHarness.Value.Start();
            }

            return _testHarness.Value;
        }
    }
}