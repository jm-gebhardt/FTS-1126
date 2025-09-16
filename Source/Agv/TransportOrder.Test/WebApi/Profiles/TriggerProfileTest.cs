using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;
using PolygonPoint = Gebhardt.Acc.TransportOrder.Domain.Models.Polygon.PolygonPoint;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class TriggerProfileTest
{
    [TestMethod]
    public void Map_NodeLocation_MapsToDtoProperties()
    {
        var mapper = MappingHelper.GetMapper(new TriggerProfile(), new PolygonPointProfile());

        var parameter = new ConfigurationParameter("parameter name", "description", "value", "validation regex");
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [parameter]);
        var aNode = "a node".ToGraphNode();
        var location = trigger.AddLocation(aNode, null, Guid.NewGuid());

        var triggerDto = mapper.Map<TriggerDto>(trigger);

        triggerDto.Name.ShouldBe(trigger.DisplayName);
        triggerDto.Id.ShouldBe(trigger.Id);
        triggerDto.Description.ShouldBe(trigger.Description);
        triggerDto.ConfigurationParameters.Count.ShouldBe(trigger.ConfigurationParameters.Count);
        triggerDto.ConfigurationParameters.First().Value.ShouldBe(trigger.ConfigurationParameters.First().Value);
        triggerDto.ConfigurationParameters.First().Validation.ShouldBe(trigger.ConfigurationParameters.First().Validation);
        triggerDto.ConfigurationParameters.First().Name.ShouldBe(trigger.ConfigurationParameters.First().Name);
        triggerDto.ConfigurationParameters.First().Description.ShouldBe(trigger.ConfigurationParameters.First().Description);
        triggerDto.Locations.Count.ShouldBe(1);
        var triggerLocationDto = triggerDto.Locations.Single();
        triggerLocationDto.NodeId.ShouldBe(aNode.Name);
        triggerLocationDto.EdgeId.ShouldBe(location.EdgeId);
        triggerLocationDto.Polygon.ShouldBeEmpty();
        triggerLocationDto.Id.ShouldBe(location.Id);
        triggerLocationDto.MapId.ShouldBe(location.MapId);
    }

    [TestMethod]
    public void Map_EdgeLocation_MapsToDtoProperties()
    {
        var mapper = MappingHelper.GetMapper(new TriggerProfile(), new PolygonPointProfile());

        var parameter = new ConfigurationParameter("parameter name", "description", "value", "validation regex");
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [parameter]);
        var aNode = "a node".ToGraphNode();
        var location = trigger.AddLocation(aNode, null, Guid.NewGuid());

        var triggerDto = mapper.Map<TriggerDto>(trigger);

        triggerDto.Name.ShouldBe(trigger.DisplayName);
        triggerDto.Id.ShouldBe(trigger.Id);
        triggerDto.Description.ShouldBe(trigger.Description);
        triggerDto.ConfigurationParameters.Count.ShouldBe(trigger.ConfigurationParameters.Count);
        triggerDto.ConfigurationParameters.First().Value.ShouldBe(trigger.ConfigurationParameters.First().Value);
        triggerDto.ConfigurationParameters.First().Validation.ShouldBe(trigger.ConfigurationParameters.First().Validation);
        triggerDto.ConfigurationParameters.First().Name.ShouldBe(trigger.ConfigurationParameters.First().Name);
        triggerDto.ConfigurationParameters.First().Description.ShouldBe(trigger.ConfigurationParameters.First().Description);
        triggerDto.Locations.Count.ShouldBe(1);
        var triggerLocationDto = triggerDto.Locations.Single();
        triggerLocationDto.NodeId.ShouldBe(aNode.Name);
        triggerLocationDto.EdgeId.ShouldBe(location.EdgeId);
        triggerLocationDto.Polygon.ShouldBeEmpty();
        triggerLocationDto.Id.ShouldBe(location.Id);
        triggerLocationDto.MapId.ShouldBe(location.MapId);
    }

    [TestMethod]
    public void Map_PolygonLocation_MapsToDtoProperties()
    {
        var mapper = MappingHelper.GetMapper(new TriggerProfile(), new PolygonPointProfile());

        var parameter = new ConfigurationParameter("parameter name", "description", "value", "validation regex");
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [parameter]);
        var location = trigger.AddLocation([new PolygonPoint(42.0, 42.0, 0, Guid.NewGuid())], Guid.NewGuid());

        var triggerDto = mapper.Map<TriggerDto>(trigger);

        triggerDto.Name.ShouldBe(trigger.DisplayName);
        triggerDto.Id.ShouldBe(trigger.Id);
        triggerDto.Description.ShouldBe(trigger.Description);
        triggerDto.ConfigurationParameters.Count.ShouldBe(trigger.ConfigurationParameters.Count);
        triggerDto.ConfigurationParameters.First().Value.ShouldBe(trigger.ConfigurationParameters.First().Value);
        triggerDto.ConfigurationParameters.First().Validation.ShouldBe(trigger.ConfigurationParameters.First().Validation);
        triggerDto.ConfigurationParameters.First().Name.ShouldBe(trigger.ConfigurationParameters.First().Name);
        triggerDto.ConfigurationParameters.First().Description.ShouldBe(trigger.ConfigurationParameters.First().Description);
        triggerDto.Locations.Count.ShouldBe(1);
        var triggerLocationDto = triggerDto.Locations.Single();
        triggerLocationDto.NodeId.ShouldBeNullOrEmpty();
        triggerLocationDto.EdgeId.ShouldBe(location.EdgeId);
        triggerLocationDto.Polygon.ShouldNotBeNull();
        triggerLocationDto.Polygon!.First().X.ShouldBe(location.Polygon.First().X);
        triggerLocationDto.Polygon!.First().Y.ShouldBe(location.Polygon.First().Y);
        triggerLocationDto.Id.ShouldBe(location.Id);
        triggerLocationDto.MapId.ShouldBe(location.MapId);
    }

    [TestMethod]
    public void Map_TriggerDtoLocation_MapsToLocation()
    {
        var mapper = MappingHelper.GetMapper(new TriggerProfile(), new PolygonPointProfile());

        var triggerLocationDto = new TriggerLocationDto { Id = Guid.NewGuid(), Polygon = [new PolygonPointDto(-1.0d, -8.0d), new PolygonPointDto(-1.0d, -8.0d), new PolygonPointDto(-1.0d, -8.0d)], MapId = Guid.NewGuid() };

        var location = mapper.Map<Location>(triggerLocationDto);

        location.MapId.ShouldBe(triggerLocationDto.MapId);
        location.Polygon.Count.ShouldBe(triggerLocationDto.Polygon.Count);
        location.Polygon.First().X.ShouldBe(triggerLocationDto.Polygon.First().X);
        location.Polygon.First().Y.ShouldBe(triggerLocationDto.Polygon.First().Y);
    }
}