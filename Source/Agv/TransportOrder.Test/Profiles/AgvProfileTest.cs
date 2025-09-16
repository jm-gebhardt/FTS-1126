using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Profiles;

[TestClass]
public class AgvProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvDataDtoIsMappedToAgvData()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvDataMessageContent = mapper.Map<TransportOrder.Messages.Requests.Agv.Queries.AgvData>(agvData);

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
        agvDataMessageContent.TurnOnSpotThreshold.ShouldBe(agvData.TurnOnSpotThreshold);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvDtoIsMappedToAgvWithAgvStatus()
    {
        var mapper = MappingHelper.GetMapper(new AgvProfile(), new GraphProfile());
        var lastNode = "current".ToGraphNode();
        var agv = AgvGenerator.GenerateIdleAgv();

        var agvMessageContent = mapper.Map<Agv>(agv, opts => { opts.Items[nameof(Agv.LastNode)] = lastNode; });
        agvMessageContent.ShouldSatisfyAllConditions(
            o => o.SerialNumber.ShouldBe(agv.SerialNumber),
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
            o => o.IsCharging.ShouldBe(agv.AgvStatus!.Charging),
            o => o.LastNode.ShouldNotBeNull(),
            o => o.LastNode!.Id.ShouldBe(lastNode.Id),
            o => o.TotalDistance.ShouldBe(agv.AgvStatus!.TotalDistance),
            o => o.TotalLifetime.ShouldBe(agv.AgvStatus!.TotalLifetime)
        );
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhenAgvErrorsListIsMappedToAgv()
    {
        var mapper = MappingHelper.GetMapper(new AgvProfile(), new AgvErrorProfile());
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvMessageContent = mapper.Map<TransportOrder.Domain.Models.Agv.Agv, Agv>(agv);
        var expectedAgvErrors = new List<AgvError>
        {
            AgvErrorGenerator.GenerateAgvError(agv.CorrelationId)
        };

        mapper.Map(agv, agvMessageContent, opts => { opts.Items[nameof(Agv.Errors)] = expectedAgvErrors; });

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