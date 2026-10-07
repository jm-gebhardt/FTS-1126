using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.MqttMessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class MqttInstantActionsConsumerTestFixture : MqttConsumerTestFixture<MqttInstantActionsConsumer>
{
    public const string SimulatedSerialNumber = "simagv0";

    public MqttInstantActionsConsumerTestFixture()
    {
        AddServices(sc =>
        {
            var operatingModeCache = new Mock<IAgvOperatingModeCache>();
            operatingModeCache.Setup(c => c.ExistsAsync(SimulatedSerialNumber.ToGuid())).ReturnsAsync(true);
            sc.AddSingleton(operatingModeCache.Object);
        });
        Build();
    }
}