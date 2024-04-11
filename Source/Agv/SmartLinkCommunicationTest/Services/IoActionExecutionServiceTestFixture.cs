using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunicationTest.Services;

public class IoActionExecutionServiceTestFixture : MassTransitTestFixture
{
    public readonly IoActionExecutionService IoActionExecutionService;

    public Mock<IBus> Bus { get; } = new();

    public IoActionExecutionServiceTestFixture()
    {
        AddServices(sc => sc.AddTransient(_ => Bus.Object));
        IoActionExecutionService = new IoActionExecutionService(Bus.Object);

        Build();
    }

    protected override void ConfigureInMemory(IInMemoryBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        base.ConfigureInMemory(cfg, context);
        cfg.AddSerializer(new SystemTextJsonRawMessageSerializerFactory());
        cfg.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
    }
}