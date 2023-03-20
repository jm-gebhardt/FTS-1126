using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

public class StartChargingCommandConsumerTestFixture : CommandConsumerTestFixture<StartChargingCommand, StartChargingCommandConsumer>
{
    private AgvInformation? GetBaseValidAgvInformation()
    {
        return AgvRegistry.GetAgvInformation("AGV004".ToGuid());
    }

    public StartChargingCommandConsumerTestFixture AddAgvInformationWithSpecificAgvStatus(Guid correlationId, AgvStatus? agvStatus)
    {
        var agvInformation = GetBaseValidAgvInformation();
        AgvRegistry.AddOrUpdate(correlationId, new AgvInformation { AgvStatusReceived = AgvRegistryTestFixture.GetAgvStatusReceived(agvStatus), AgvGraph = agvInformation?.AgvGraph, AgvAction = agvInformation?.AgvAction });
        return this;
    }

    public AgvInformation? GetAgvInformation(Guid correlationId)
    {
        return AgvRegistry.GetAgvInformation(correlationId);
    }

    public override StartChargingCommandConsumerTestFixture Build()
    {
        UseDefaultServices();
        base.Build();
        return this;
    }
}