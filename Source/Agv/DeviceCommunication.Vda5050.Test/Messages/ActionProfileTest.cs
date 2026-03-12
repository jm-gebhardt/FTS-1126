using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Messages;

[TestClass]
public class ActionProfileTest
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void SetUp()
    {
        _mapper = MappingHelper.GetMapper(new AgvActionProfile());
    }

    [TestMethod]
    public void Map_AgvActionToAgvInstantAction_MapsAllProperties()
    {
        var agvAction = AgvGenerator.InitPositionInstantAction;

        var agvInstantAction = _mapper.Map<AgvInstantAction>(agvAction);

        agvInstantAction.ActionType.ShouldBe(agvAction.ActionType);
        agvInstantAction.Description.ShouldBe(agvAction.ActionDescription);
        agvInstantAction.ResultDescription.ShouldBe(string.Empty);
        agvInstantAction.BlockingTypes.ShouldBeEquivalentTo(new List<BlockingType>{BlockingType.HARD});
        var xParameter = agvInstantAction.Parameters!.Single(p => p.Key == Vda5050Specifications.Parameters.X);
        xParameter.Description.ShouldBe(string.Empty);
        xParameter.ValueType.ShouldBe(nameof(ValueDataType.Float));
        xParameter.IsOptional.ShouldBeFalse();
        var yParameter = agvInstantAction.Parameters!.Single(p => p.Key == Vda5050Specifications.Parameters.Y);
        yParameter.Description.ShouldBe(string.Empty);
        yParameter.ValueType.ShouldBe(nameof(ValueDataType.Float));
        yParameter.IsOptional.ShouldBeFalse();
        var theta = agvInstantAction.Parameters!.Single(p => p.Key == Vda5050Specifications.Parameters.Theta);
        theta.Description.ShouldBe(string.Empty);
        theta.ValueType.ShouldBe(nameof(ValueDataType.Float));
        theta.IsOptional.ShouldBeFalse();
        var mapId = agvInstantAction.Parameters!.Single(p => p.Key == Vda5050Specifications.Parameters.MapId);
        mapId.Description.ShouldBe(string.Empty);
        mapId.ValueType.ShouldBe(nameof(ValueDataType.String));
        mapId.IsOptional.ShouldBeFalse();
        var lastNodeId = agvInstantAction.Parameters!.Single(p => p.Key == Vda5050Specifications.Parameters.LastNodeId);
        lastNodeId.Description.ShouldBe(string.Empty);
        lastNodeId.ValueType.ShouldBe(nameof(ValueDataType.String));
        lastNodeId.IsOptional.ShouldBeFalse();
    }
}