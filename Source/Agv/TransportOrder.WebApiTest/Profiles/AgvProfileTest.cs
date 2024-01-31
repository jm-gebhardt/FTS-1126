using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvErrorProfile = Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.AgvErrorProfile;
using AgvProfile = Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.AgvProfile;
using AgvSettings = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.AgvSettings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Profiles;

[TestClass]
public class AgvProfileTest
{
    private static Mapper GetMapper() => MappingHelper.GetMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvGraphProfile());

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvIsMappedToAgvDto()
    {
        var correlationId = Guid.NewGuid();
        const string agvSerialNumber = "TestAgv-01";
        const string orderId = "TestOrder-01";
        const string lastNodeId = "WAY3";
        var stateTransitionTimestamp = DateTime.UtcNow;

        var actionGroup = new ActionGroupReadModel(correlationId, orderId, correlationId, stateTransitionTimestamp, null, null, TimeSpan.FromHours(99), ActionGroupState.Created, ActionGroupCreator.TransportOrderWebApi, new List<AgvActionReadModel>(), false, new List<Reason>());
        var agvMessageContent = new Messages.MessageContent.Agv(correlationId, agvSerialNumber, agvSerialNumber, AgvState.Idle, stateTransitionTimestamp, DateTime.MinValue, true, false, false, true, 50d, false, actionGroup, new List<AgvError>(), lastNodeId, new Messages.MessageContent.GraphNode {Id = lastNodeId, Type = AgvGraph.NodeType.WayPoint, X = 1.0f, Y = 2.0f, Z = 0.0f, Theta = 0.0f});

        var mapper = GetMapper();

        var agvDto = mapper.Map<AgvDto>(agvMessageContent);
        agvDto.SerialNumber.ShouldBe(agvSerialNumber);
        agvDto.CorrelationId.ShouldBe(correlationId);
        agvDto.CurrentState.ShouldBe(AgvStateType.Idle);
        agvDto.TimeOfStateTransition.ShouldBe(stateTransitionTimestamp);
        agvDto.TimeOfLockStateChanged.ShouldBe(DateTime.MinValue);
        agvDto.IsOnline.ShouldBeTrue();
        agvDto.IsLocked.ShouldBeFalse();
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

        var dto = mapper.Map<AgvSettingsDto>(agvSettings);

        dto.CorrelationId.ShouldBe(agvSettings.CorrelationId);
        dto.BatteryLevelCritical.ShouldBe(agvSettings.BatteryLevelCritical);
        dto.BatteryLevelLow.ShouldBe(agvSettings.BatteryLevelLow);
        dto.MinChargeUp.ShouldBe(agvSettings.MinChargeUp);
        dto.MaxChargeUp.ShouldBe(agvSettings.MaxChargeUp);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvBatteryChargeChangedIsMappedToAgvBatteryStatusDto()
    {
        var agvBatteryChargeChanged = new AgvBatteryChargeChanged(Guid.NewGuid(), 1, 2, 3, 4, 5, 6, true, 7, 8, 9, null, null);

        var mapper = GetMapper();

        var dto = mapper.Map<AgvBatteryStatusDto>(agvBatteryChargeChanged);
        dto.CorrelationId.ShouldBe(agvBatteryChargeChanged.CorrelationId);
        dto.BatteryCharge.ShouldBe(agvBatteryChargeChanged.NewBatteryCharge);
        dto.BatteryLevelCritical.ShouldBe(agvBatteryChargeChanged.BatteryLevelCritical);
        dto.BatteryLevelLow.ShouldBe(agvBatteryChargeChanged.BatteryLevelLow);
        dto.MinChargeUp.ShouldBe(agvBatteryChargeChanged.MinChargeUp);
        dto.MaxChargeUp.ShouldBe(agvBatteryChargeChanged.MaxChargeUp);
        dto.IsCharging.ShouldBe(agvBatteryChargeChanged.IsCharging);
        dto.BatteryVoltage.ShouldBe(agvBatteryChargeChanged.BatteryVoltage);
        dto.BatteryCurrent.ShouldBe(agvBatteryChargeChanged.BatteryCurrent);
        dto.BatteryTemperature.ShouldBe(agvBatteryChargeChanged.BatteryTemperature);
        dto.EstimatedFullyChargedTime.ShouldBe(agvBatteryChargeChanged.EstimatedFullyChargedTime);
        dto.LastBatteryReplacement.ShouldBe(agvBatteryChargeChanged.LastBatteryReplacement);
    }
}