using JetBrains.Annotations;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;

/// <summary>
/// Testing fixture to test MQTT.
/// </summary>
[PublicAPI]
public class MqttTestFixture : MassTransitTestFixture
{
    public MqttTestFixture()
    {
        Build();
    }

    protected override void ConfigureInMemory(IInMemoryBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        base.ConfigureInMemory(cfg, context);
        ConfigureMqtt(cfg);
    }

    public static void ConfigureMqtt(IInMemoryBusFactoryConfigurator cfg)
    {
        cfg.AddSerializer(new SystemTextJsonRawMessageSerializerFactory());
        cfg.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
    }
}