using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

public class AgvRegistryTestFixture
{
    public IAgvRegistry AgvRegistry { get; } = new AgvRegistry();

    public AgvRegistryTestFixture WithAgvInformation(AgvInformation agvInformation)
    {
        AgvRegistry.AddOrUpdate(agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId, agvInformation);
        return this;
    }
}