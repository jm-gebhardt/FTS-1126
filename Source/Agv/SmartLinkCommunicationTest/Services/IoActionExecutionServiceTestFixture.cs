using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.SmartLinkCommunication.Services;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.SmartLinkCommunicationTest.Services;

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