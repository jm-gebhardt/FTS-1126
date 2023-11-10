using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

// ReSharper disable ConvertConstructorToMemberInitializers
public class AgvRepositoryTestFixture
{
    public IAgvRepository AgvRepository { get; } = new AgvRepository();

    public AgvRepositoryTestFixture WithAgvInformation(AgvInformation agvInformation)
    {
        AgvRepository.AddOrUpdate(agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId, agvInformation);
        return this;
    }
}