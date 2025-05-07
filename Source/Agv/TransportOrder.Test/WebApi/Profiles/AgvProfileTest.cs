using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvErrorProfile = Gebhardt.Acc.TransportOrder.WebApi.Profiles.AgvErrorProfile;
using AgvProfile = Gebhardt.Acc.TransportOrder.WebApi.Profiles.AgvProfile;
using AgvSettings = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvSettings;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class AgvProfileTest
{
    private static Mapper GetMapper() => MappingHelper.GetMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvGraphProfile(), new AgvSettingsProfile());

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvIsMappedToAgvDto()
    {
        var correlationId = Guid.NewGuid();
        const string agvSerialNumber = "TestAgv-01";
        const string orderId = "TestOrder-01";
        const string lastNodeId = "WAY3";
        var stateTransitionTimestamp = DateTime.UtcNow;

        var actionGroup = new ActionGroupReadModel(correlationId, orderId, correlationId, stateTransitionTimestamp, null, null, TimeSpan.FromHours(99), ActionGroupState.Created, ActionGroupCreator.TransportOrderWebApi, "hu",new List<AgvActionReadModel>(), false, false, true, new List<Reason>());
        var agvMessageContent = new TransportOrder.Messages.MessageContent.Agv(correlationId, agvSerialNumber, agvSerialNumber, AgvState.Idle, stateTransitionTimestamp, DateTime.MinValue, true, false, false, true, 50d, false, actionGroup, new List<AgvError>(), lastNodeId, new TransportOrder.Messages.MessageContent.GraphNode {Id = lastNodeId, Type = AgvGraph.NodeType.WayPoint, X = 1.0f, Y = 2.0f, Z = 0.0f, Theta = 0.0f}, new AgvMode(ModeType.Normal, correlationId));

        var mapper = GetMapper();

        var agvDto = mapper.Map<AgvDto>(agvMessageContent);
        agvDto.SerialNumber.ShouldBe(agvSerialNumber);
        agvDto.CorrelationId.ShouldBe(correlationId);
        agvDto.CurrentState.ShouldBe(AgvStateType.Idle);
        agvDto.TimeOfStateTransition.ShouldBe(stateTransitionTimestamp);
        agvDto.TimeOfLockStateChanged.ShouldBe(DateTime.MinValue);
        agvDto.IsOnline.ShouldBeTrue();
        agvDto.IsLocked.ShouldBeFalse();
        agvDto.AgvMode.Type.ShouldBe(ModeType.Normal);
        agvDto.IsDisturbed.ShouldBeFalse();
        agvDto.IsAutomatic.ShouldBeTrue();
        agvDto.BatteryCharge.ShouldBe(50d);
        agvDto.IsCharging.ShouldBeFalse();
        agvDto.Errors.ShouldBeEmpty();
        agvDto.CurrentActionGroup.ShouldNotBeNull();
        agvDto.CurrentActionGroup.OrderId.ShouldBe(orderId);
        agvDto.CurrentActionGroup.AssignedAgv.ShouldBe(correlationId);
        agvDto.CurrentActionGroup.CreatedDate.ShouldBe(stateTransitionTimestamp);
        agvDto.CurrentActionGroup.StartedDate.ShouldBeNull();
        agvDto.CurrentActionGroup.CompletedDate.ShouldBeNull();
        agvDto.CurrentActionGroup.State.ShouldBe(ActionGroupState.Created);
        agvDto.CurrentActionGroup.Creator.ShouldBe(ActionGroupCreator.TransportOrderWebApi);
        agvDto.CurrentActionGroup.IsManual.ShouldBeFalse();
        agvDto.CurrentActionGroup.Actions.ShouldBeEmpty();
        agvDto.LastNode.ShouldNotBeNull();
        agvDto.LastNode.Id.ShouldBe(lastNodeId);
        agvDto.LastNode.Type.ShouldBe(AgvGraph.NodeType.WayPoint);
        agvDto.LastNode.X.ShouldBe(1.0f);
        agvDto.LastNode.Y.ShouldBe(2.0f);
        agvDto.LastNode.Z.ShouldBe(0.0f);
        agvDto.LastNode.Theta.ShouldBe(0.0f);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvSettingsIsMappedToAgvSettingsDto()
    {
        var agvSettings = new AgvSettings();
        var mapper = GetMapper();

        var dto = mapper.Map<AgvSettingsResponseDto>(agvSettings);

        dto.CorrelationId.ShouldBe(agvSettings.CorrelationId);
        dto.BatteryLevelCritical.ShouldBe(agvSettings.BatteryLevelCritical);
        dto.BatteryLevelLow.ShouldBe(agvSettings.BatteryLevelLow);
        dto.MinChargeUp.ShouldBe(agvSettings.MinChargeUp);
        dto.MaxChargeUp.ShouldBe(agvSettings.MaxChargeUp);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvBatteryStatusIsMappedToAgvBatteryStatusDto()
    {
        var agvBatteryStatus = new AgvBatteryStatus(Guid.NewGuid(), 2, true, 7, 8, 9, 3, 4, 5, 6, null, null);

        var mapper = GetMapper();

        var dto = mapper.Map<AgvBatteryStatusDto>(agvBatteryStatus);
        dto.CorrelationId.ShouldBe(agvBatteryStatus.CorrelationId);
        dto.BatteryCharge.ShouldBe(agvBatteryStatus.BatteryCharge);
        dto.BatteryLevelCritical.ShouldBe(agvBatteryStatus.BatteryLevelCritical);
        dto.BatteryLevelLow.ShouldBe(agvBatteryStatus.BatteryLevelLow);
        dto.MinChargeUp.ShouldBe(agvBatteryStatus.MinChargeUp);
        dto.MaxChargeUp.ShouldBe(agvBatteryStatus.MaxChargeUp);
        dto.IsCharging.ShouldBe(agvBatteryStatus.IsCharging);
        dto.BatteryVoltage.ShouldBe(agvBatteryStatus.BatteryVoltage);
        dto.BatteryCurrent.ShouldBe(agvBatteryStatus.BatteryCurrent);
        dto.BatteryTemperature.ShouldBe(agvBatteryStatus.BatteryTemperature);
        dto.EstimatedFullyChargedTime.ShouldBe(agvBatteryStatus.EstimatedFullyChargedTime);
        dto.LastBatteryReplacement.ShouldBe(agvBatteryStatus.LastBatteryReplacement);
    }

    [TestMethod]
    public void Map_WhenBatterySettingsAreNull_ShouldMapToNegativeValue()
    {
        var settingsDto = new AgvSettingsDto(12);

        var mapper = MappingHelper.GetMapper(new AgvSettingsProfile());
        var settings = mapper.Map<AgvSettingsDto, AgvSettingsReadModel>(settingsDto);
        settings.BatteryLevelCritical.ShouldBe((int)settingsDto.BatteryLevelCritical!);
        settings.BatteryLevelLow.ShouldBeLessThan(0);
        settings.MinChargeUp.ShouldBeLessThan(0);
        settings.MaxChargeUp.ShouldBeLessThan(0);
    }
}