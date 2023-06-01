using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

[TestClass]
public class AgvProfileTest
{
    /// <summary>
    /// Maps the <see cref="AgvSaga"/> to <see cref="AgvDto"/> and should map correctly
    /// </summary>
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvSagaIsMappedToAgv()
    {
        const string agvId = "AGV1";
        var correlationId = agvId.ToGuid();
        const string mapName = "Map#1";
        var agvSaga = new AgvSaga(correlationId, agvId, "Idle",
            new AgvStatus(Guid.NewGuid(), "123", true, true, 10, false, false),
            new AgvGraph(Guid.NewGuid(),
                new AgvGraph.Node[] { new("id") },
                new AgvGraph.Edge[] { new("from", "to") },
                mapName.ToGuid(),
                mapName)
        );

        var mapper = new MappingProfileTestFixture().GetMapper(new[] { new AgvProfile() });
        var agv = mapper.Map<IAgv, AgvDto>(agvSaga);

        agv.ShouldNotBeNull();
        agv.CorrelationId.ShouldBe(agvSaga.CorrelationId);
        agv.AgvId.ShouldBe(agvSaga.AgvId);
        Enum.TryParse(agvSaga.CurrentState, out AgvState agvState);
        agv.CurrentState.ShouldBe(agvState);
    }
    
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvDataDtoIsMappedToAgvData()
    {
        var agvData = new AgvData(Guid.NewGuid(), "Karis", "BF1", "127.0.0.1", 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());
        var mapper = new MappingProfileTestFixture().GetMapper(new[] { new AgvProfile() });
        var agvDataDto = mapper.Map<AgvDataDto>(agvData);

        agvDataDto.ShouldNotBeNull();
        agvDataDto.Acceleration.ShouldBe(agvData.Acceleration);
        agvDataDto.CorrelationId.ShouldBe(agvData.CorrelationId);
        agvDataDto.BatteryCapacity.ShouldBe(agvData.BatteryCapacity);
        agvDataDto.IpV4Address.ShouldBe(agvData.IpV4Address);
        agvDataDto.Length.ShouldBe(agvData.Length);
        agvDataDto.MaxVelocityStraight.ShouldBe(agvData.MaxVelocityStraight);
        agvDataDto.MaxVelocityTurn.ShouldBe(agvData.MaxVelocityTurn);
        agvDataDto.MaxVelocityTurnOnSpot.ShouldBe(agvData.MaxVelocityTurnOnSpot);
        agvDataDto.Model.ShouldBe(agvData.Model);
        agvDataDto.Payload.ShouldBe(agvData.Payload);
        agvDataDto.Width.ShouldBe(agvData.Width);
        agvDataDto.Sensors.ShouldBe(agvData.Sensors);
    }
}