using Gebhardt.Acc.DeviceCommunication.Services;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services;

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