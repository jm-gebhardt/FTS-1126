using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

public class StopChargingCommandConsumerTestFixture : CommandConsumerTestFixture<StopChargingCommand, StopChargingCommandConsumer>
{
    private AgvInformation? GetBaseValidAgvInformation()
    {
        return AgvRegistry.GetAgvInformation("AGV003".ToGuid());
    }

    public StopChargingCommandConsumerTestFixture AddAgvInformationWithSpecificAgvStatus(Guid correlationId, AgvStatus? agvStatus)
    {
        var agvInformation = GetBaseValidAgvInformation();
        AgvRegistry.AddOrUpdate(correlationId, new AgvInformation { AgvStatusReceived = AgvRegistryTestFixture.GetAgvStatusReceived(agvStatus), AgvGraph = agvInformation?.AgvGraph, AgvAction = agvInformation?.AgvAction });
        return this;
    }

    public override MediatorTestFixture<StopChargingCommand, StopChargingCommandConsumer> Build()
    {
        UseDefaultServices();
        return base.Build();
    }
}