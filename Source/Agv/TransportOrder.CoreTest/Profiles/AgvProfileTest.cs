using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvStatus = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

[TestClass]
public class AgvProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvDataDtoIsMappedToAgvData()
    {
        var agvData = new AgvData(Guid.NewGuid(), "Karis", "BF1", "127.0.0.1", 1.8, 120.0, 0.6, 0.8, 0.7, null, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());
        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvDataMessageContent = mapper.Map<Messages.Requests.Agv.Queries.AgvData>(agvData);

        agvDataMessageContent.ShouldNotBeNull();
        agvDataMessageContent.Acceleration.ShouldBe(agvData.Acceleration);
        agvDataMessageContent.CorrelationId.ShouldBe(agvData.CorrelationId);
        agvDataMessageContent.BatteryCapacity.ShouldBe(agvData.BatteryCapacity);
        agvDataMessageContent.IpV4Address.ShouldBe(agvData.IpV4Address);
        agvDataMessageContent.Length.ShouldBe(agvData.Length);
        agvDataMessageContent.MaxVelocityStraight.ShouldBe(agvData.MaxVelocityStraight);
        agvDataMessageContent.MaxVelocityTurn.ShouldBe(agvData.MaxVelocityTurn);
        agvDataMessageContent.MaxVelocityTurnOnSpot.ShouldBe(agvData.MaxVelocityTurnOnSpot);
        agvDataMessageContent.Model.ShouldBe(agvData.Model);
        agvDataMessageContent.Payload.ShouldBe(agvData.Payload);
        agvDataMessageContent.Width.ShouldBe(agvData.Width);
        agvDataMessageContent.Sensors.ShouldBe(agvData.Sensors);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvDtoIsMappedToAgvWithAgvStatus()
    {
        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvStatus = new AgvStatus(Guid.NewGuid(), "OrderId#123", false, true, 98, false, false, "lastNodeId#123");
        var agv = AgvGenerator.GenerateIdleAgv(agvStatus);

        var agvMessageContent = mapper.Map<Messages.MessageContent.Agv>(agv);
        agvMessageContent.ShouldSatisfyAllConditions(
            o => o.AgvId.ShouldBe(agv.AgvId),
            o => o.CorrelationId.ShouldBe(agv.CorrelationId),
            o => o.CurrentState.ShouldBe(agv.CurrentAgvState),
            o => o.TimeOfStateTransition.ShouldBe(agv.TimeOfStateTransition),
            o => o.TimeOfLockStateChanged.ShouldBe(agv.TimeOfLockStateChanged),
            o => o.TimeOfLockStateChanged.ShouldBe(agv.TimeOfLockStateChanged),
            o => o.IsOnline.ShouldBe(agv.IsOnline),
            o => o.IsLocked.ShouldBe(agv.IsLocked),
            o => o.IsDisturbed.ShouldBe(agv.AgvStatus!.Disturbed),
            o => o.IsAutomatic.ShouldBe(agv.AgvStatus!.Automatic),
            o => o.BatteryCharge.ShouldBe(agv.AgvStatus!.BatteryCharge),
            o => o.IsCharging.ShouldBe(agv.AgvStatus!.Charging)
        );
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhenAgvErrorsListIsMappedToAgv()
    {
        var mapper = MappingHelper.GetMapper(new AgvProfile(), new AgvErrorProfile());
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvMessageContent = mapper.Map<TransportOrder.Domain.Models.Agv.Agv, Messages.MessageContent.Agv>(agv);
        var expectedAgvErrors = new List<AgvError>
        {
            AgvErrorGenerator.GenerateAgvError(agv.CorrelationId)
        };

        mapper.Map(expectedAgvErrors, agvMessageContent);

        foreach (var agvMessageContentError in agvMessageContent.Errors)
        {
            agvMessageContentError.AgvCorrelationId.ShouldBe(agvMessageContentError.AgvCorrelationId);
            var expectedAgvError = expectedAgvErrors.Find(e => e.ErrorType == agvMessageContentError.ErrorType);
            expectedAgvError.ShouldNotBeNull();
            agvMessageContentError.ErrorDescription.ShouldBe(expectedAgvError.ErrorDescription);
            agvMessageContentError.ErrorLevel.ShouldBe(expectedAgvError.ErrorLevel);
            agvMessageContentError.ErrorType.ShouldBe(expectedAgvError.ErrorType);
            agvMessageContentError.ErrorReferences.ShouldBe(expectedAgvError.ErrorReferences);
            agvMessageContentError.ErrorReceived.ShouldBe(expectedAgvError.ErrorReceived);
            agvMessageContentError.CorrelationId.ShouldBe(expectedAgvError.CorrelationId);
            agvMessageContentError.ErrorResolved.ShouldBe(expectedAgvError.ErrorResolved);
        }
    }
}