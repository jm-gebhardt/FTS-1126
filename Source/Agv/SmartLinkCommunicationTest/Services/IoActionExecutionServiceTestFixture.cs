using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace SmartLinkCommunicationTest.Services;

public class IoActionExecutionServiceTestFixture
{
    private readonly List<Action<IServiceCollection>> _serviceActions = new();
    
    /// <summary>
    /// Service provider
    /// </summary>
    private ServiceProvider? _provider;

    /// <summary>
    /// Started TestHarness
    /// </summary>
    private ITestHarness? _testHarness;
    
    public readonly IoActionExecutionService IoActionExecutionService;

    public Mock<IBus> Bus { get; } = new();

    /// <summary>
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="services">The action of the service to add</param>
    /// <returns></returns>
    private void AddServices(Action<IServiceCollection> services)
    {
        _serviceActions.Add(services);
    }
    
    public IoActionExecutionServiceTestFixture()
    {
        AddServices(sc => sc.AddTransient(_ => Bus.Object));
        IoActionExecutionService = new IoActionExecutionService(Bus.Object);
    }
    
    public IoActionExecutionServiceTestFixture Build()
    {
        var collection = new ServiceCollection().AddMassTransitTestHarness(x =>
        {
            x.UsingInMemory((context, cfg) =>
            {
                cfg.AddSerializer(new SystemTextJsonRawMessageSerializerFactory());
                cfg.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
                cfg.ConfigureEndpoints(context);
            });
        });

        foreach (Action<IServiceCollection> service in _serviceActions)
        {
            service.Invoke(collection);
        }

        _provider = collection.BuildServiceProvider(true);
        _testHarness = _provider.GetTestHarness();
        _testHarness.Start();

        return this;
    }
}