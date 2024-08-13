using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class TriggerProfileTest
{
    [TestMethod]
    public void Map_NodeLocation_MapsToDtoProperties()
    {
        var mapper = MappingHelper.GetMapper(new TriggerProfile());

        var parameter = new ConfigurationParameter("parameter name", "description", "value", "validation regex");
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [parameter]);
        var location = trigger.AddLocation("a node", Guid.NewGuid());

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
        triggerDto.Locations.First().NodeId.ShouldBe(location.NodeId);
        triggerDto.Locations.First().EdgeId.ShouldBe(location.EdgeId);
        triggerDto.Locations.First().Polygon.ShouldBeEmpty();
        triggerDto.Locations.First().Id.ShouldBe(location.Id);
        triggerDto.Locations.First().MapId.ShouldBe(location.MapId);
    }

    [TestMethod]
    public void Map_EdgeLocation_MapsToDtoProperties()
    {
        var mapper = MappingHelper.GetMapper(new TriggerProfile());

        var parameter = new ConfigurationParameter("parameter name", "description", "value", "validation regex");
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [parameter]);
        var location = trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());

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
        triggerDto.Locations.First().NodeId.ShouldBe(location.NodeId);
        triggerDto.Locations.First().EdgeId.ShouldBe(location.EdgeId);
        triggerDto.Locations.First().Polygon.ShouldBeEmpty();
        triggerDto.Locations.First().Id.ShouldBe(location.Id);
        triggerDto.Locations.First().MapId.ShouldBe(location.MapId);
    }

    [TestMethod]
    public void Map_PolygonLocation_MapsToDtoProperties()
    {
        var mapper = MappingHelper.GetMapper(new TriggerProfile());

        var parameter = new ConfigurationParameter("parameter name", "description", "value", "validation regex");
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [parameter]);
        var location = trigger.AddLocation([new PolygonPoint(42.0, 42.0, Guid.NewGuid())], Guid.NewGuid());

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
        triggerDto.Locations.First().NodeId.ShouldBe(location.NodeId);
        triggerDto.Locations.First().EdgeId.ShouldBe(location.EdgeId);
        triggerDto.Locations.First().Polygon.First().X.ShouldBe(location.Polygon.First().X);
        triggerDto.Locations.First().Polygon.First().Y.ShouldBe(location.Polygon.First().Y);
        triggerDto.Locations.First().Id.ShouldBe(location.Id);
        triggerDto.Locations.First().MapId.ShouldBe(location.MapId);
    }
}