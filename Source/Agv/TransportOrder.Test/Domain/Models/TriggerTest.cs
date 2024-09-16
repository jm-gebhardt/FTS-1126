using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using PolygonPoint = Gebhardt.Acc.TransportOrder.Domain.Models.Polygon.PolygonPoint;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class TriggerTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var id = Guid.NewGuid();
        const string description = "description";
        const string version = "version";
        const string urn = "unique resource name";
        const string displayName = "name";
        const int instances = 2;
        var configurationParameter = new ConfigurationParameter("name", "description", "value", "validation regex");

        var trigger = new Trigger(id, urn, displayName, version, description, instances, [configurationParameter]);

        trigger.Id.ShouldBe(id);
        trigger.Locations.ShouldBeEmpty();
        trigger.InstanceMaximum.ShouldBe(instances);
        trigger.ConfigurationParameters.ShouldBeEquivalentTo(new List<ConfigurationParameter>{configurationParameter});
        trigger.DisplayName.ShouldBe(displayName);
        trigger.Version.ShouldBe(version);
    }

    [TestMethod]
    public void AddLocation_NodeId_LocationAdded()
    {
        const string node = "a node id";
        var map = Guid.NewGuid();
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [new ConfigurationParameter("parameter name", "description", "value", "validation regex")]);
       
        var location = trigger.AddLocation(node, map);
        
        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBeNull();
        location.Polygon.ShouldBeEmpty();
        location.NodeId.ShouldBe(node);
        location.MapId.ShouldBe(map);
    }

    [TestMethod]
    public void AddLocation_NodeId_WithParametersNoCustomParameterGiven_LocationAddedWithParameterValuesFromDefault()
    {
        const string node = "a node id";
        var map = Guid.NewGuid();
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        
        var location = trigger.AddLocation(node, map);
        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBeNull();
        location.Polygon.ShouldBeEmpty();
        location.NodeId.ShouldBe(node);
        location.MapId.ShouldBe(map);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).ShouldBeEquivalentTo(parameter);
    }

    [TestMethod]
    public void AddLocation_NodeId_WithParametersCustomParameterGiven_LocationAddedWithCustomParameterValues()
    {
        const string custom = "custom";
        const string node = "a node id";
        var map = Guid.NewGuid();
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var location = trigger.AddLocation(node, map, [new ConfigurationParameter(parameter.Name, parameter.Description, custom, parameter.Value)]);
        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBeNull();
        location.Polygon.ShouldBeEmpty();
        location.NodeId.ShouldBe(node);
        location.MapId.ShouldBe(map);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).ShouldNotBe(parameter);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(custom);
    }

    [TestMethod]
    public void AddLocation_NodeId_InstanceMaximumExceeded_ThrowsTriggerInstanceCountException()
    {
        const int instances = 1;
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", instances, [new ConfigurationParameter("parameter name", "description", "value", "validation regex")]);
        trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(instances);

        Should.Throw<TriggerInstanceCountException>(() =>trigger.AddLocation("a node", Guid.NewGuid()));
    }

    [TestMethod]
    public void AddLocation_EdgeId_WithParametersNoCustomParameterGiven_LocationAddedWithParameterValuesFromDefault()
    {
        var edge = Guid.NewGuid();
        var map = Guid.NewGuid(); 
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        var location = trigger.AddLocation(edge, map);

        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBe(edge);
        location.Polygon.ShouldBeEmpty();
        location.NodeId.ShouldBeNull();
        location.MapId.ShouldBe(map);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).ShouldBeEquivalentTo(parameter);
    }

    [TestMethod]
    public void AddLocation_EdgeId_LocationAdded()
    {
        var edge = Guid.NewGuid();
        var map = Guid.NewGuid();
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [new ConfigurationParameter("parameter name", "description", "value", "validation regex")]);

        var location = trigger.AddLocation(edge, map);

        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBe(edge);
        location.Polygon.ShouldBeEmpty();
        location.NodeId.ShouldBeNull();
        location.MapId.ShouldBe(map);
    }

    [TestMethod]
    public void AddLocation_EdgeId_WithParametersCustomParameterGiven_LocationAddedWithCustomParameterValues()
    {
        const string custom = "custom";
        var edge = Guid.NewGuid();
        var map = Guid.NewGuid();
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        var location = trigger.AddLocation(edge, map, [new ConfigurationParameter(parameter.Name, parameter.Description, custom, parameter.Value)]);

        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBe(edge);
        location.Polygon.ShouldBeEmpty();
        location.NodeId.ShouldBeNull();
        location.MapId.ShouldBe(map);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).ShouldNotBe(parameter);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(custom);
    }

    [TestMethod]
    public void AddLocation_EdgeId_InstanceMaximumExceeded_ThrowsTriggerInstanceCountException()
    {
        const int instances = 1;
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", instances, [new ConfigurationParameter("parameter name", "description", "value", "validation regex")]);
        trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(instances);

        Should.Throw<TriggerInstanceCountException>(() => trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid()));
    }

    [TestMethod]
    public void AddLocation_Polygon_WithParametersNoCustomParameterGiven_LocationAddedWithParameterValuesFromDefault()
    {
        List<PolygonPoint> polygon = [new(42.0, 42.0, 0, Guid.NewGuid())];
        var map = Guid.NewGuid();
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        var location = trigger.AddLocation(polygon, map);

        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBeNull();
        location.Polygon.First().X.ShouldBe(polygon.First().X);
        location.Polygon.First().Y.ShouldBe(polygon.First().Y);
        location.NodeId.ShouldBeNull();
        location.MapId.ShouldBe(map);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).ShouldBeEquivalentTo(parameter);
    }

    [TestMethod]
    public void AddLocation_Polygon_WithParametersCustomParameterGiven_LocationAddedWithCustomParameterValues()
    {
        const string custom = "custom";
        List<PolygonPoint> polygon = [new(42.0, 42.0, 0, Guid.NewGuid())];
        var map = Guid.NewGuid();
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        var location = trigger.AddLocation(polygon, map, [new ConfigurationParameter(parameter.Name, parameter.Description, custom, parameter.Value)]);

        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBeNull();
        location.Polygon.First().X.ShouldBe(polygon.First().X);
        location.Polygon.First().Y.ShouldBe(polygon.First().Y);
        location.NodeId.ShouldBeNull();
        location.MapId.ShouldBe(map);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).ShouldNotBe(parameter);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(custom);
    }

    [TestMethod]
    public void AddLocation_Polygon_LocationAdded()
    {
        List<PolygonPoint> polygon = [new(42.0, 42.0, 0, Guid.NewGuid())];
        var map = Guid.NewGuid();
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [new ConfigurationParameter("parameter name", "description", "value", "validation regex")]);

        var location = trigger.AddLocation(polygon, map);

        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBeNull();
        location.Polygon.First().X.ShouldBe(polygon.First().X);
        location.Polygon.First().Y.ShouldBe(polygon.First().Y);
        location.NodeId.ShouldBeNull();
        location.MapId.ShouldBe(map);
    }

    [TestMethod]
    public void AddLocation_Polygon_InstanceMaximumExceeded_ThrowsTriggerInstanceCountException()
    {
        const int instances = 1;
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", instances, [new ConfigurationParameter("parameter name", "description", "value", "validation regex")]);
        trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(instances);

        Should.Throw<TriggerInstanceCountException>(() => trigger.AddLocation([new PolygonPoint(42.0, 42.0, 0, Guid.NewGuid())], Guid.NewGuid()));
    }

    [TestMethod]
    public void RemoveLocation_ValidData_LocationRemoved()
    {
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [new ConfigurationParameter("parameter name", "description", "value", "validation regex")]);
        var location = trigger.AddLocation("a node", Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(1);

        trigger.RemoveLocation(location.Id);
        trigger.Locations.ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveLocation_LocationNotFound_ThrowsTriggerLocationNotFoundException()
    {
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [new ConfigurationParameter("parameter name", "description", "value", "validation regex")]);

        Should.Throw<TriggerLocationNotFoundException>(() => trigger.RemoveLocation("unknown".ToGuid()));
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_NoValidation_ReturnsTrue()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "any new value").ShouldBeTrue();
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_UnknownParameter_ReturnsFalse()
    {
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.ValidateConfigurationParameterValue("name", " value").ShouldBeFalse();
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_DoesNotValidate_ReturnsFalse()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "0", isNumber);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "not a number").ShouldBeFalse();
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_ValidationIsNotValidRegex_ReturnsTrue()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", "([");
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "any new value").ShouldBeTrue();
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_Validates_ReturnsTrue()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "0", isNumber);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "12").ShouldBeTrue();
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_EscapedSpecialBackslash_ReturnsTrue()
    {
        const string isNumber = "^-?\\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "0", isNumber);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "12").ShouldBeTrue();
    }

    [TestMethod]
    public void UpdateDefaultConfigurationParameter_ValidData_UpdatesParameterValue()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        const string newValue = "a new value";

        var updatedParameter = trigger.UpdateDefaultConfigurationParameter(parameter.Name, newValue);
        
        updatedParameter.ShouldNotBeNull();
        updatedParameter.Name.ShouldBe(parameter.Name);
        updatedParameter.Validation.ShouldBe(parameter.Validation);
        updatedParameter.Value.ShouldBe(newValue);
    }

    [TestMethod]
    public void UpdateDefaultConfigurationParameter_WithLocations_UpdatesParameterValueAtLocationsThatUseDefault()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        var locationThatUsesDefault = trigger.AddLocation("a node", Guid.NewGuid());
        var locationWithCustomValue = trigger.AddLocation("another node", Guid.NewGuid());
        trigger.UpdateConfigurationParameterAtLocation(locationWithCustomValue.Id, parameter.Name, "different value");

        trigger.Locations.Single(l => l.Id == locationThatUsesDefault.Id).ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(parameter.Value);
        trigger.Locations.Single(l => l.Id == locationWithCustomValue.Id).ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldNotBe(parameter.Value);
        
        const string newValue = "a new value";
        var updatedParameter = trigger.UpdateDefaultConfigurationParameter(parameter.Name, newValue);

        updatedParameter.ShouldNotBeNull();
        updatedParameter.Name.ShouldBe(parameter.Name);
        updatedParameter.Validation.ShouldBe(parameter.Validation);
        updatedParameter.Value.ShouldBe(newValue);
        trigger.Locations.Single(l => l.Id == locationThatUsesDefault.Id).ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(newValue);
        trigger.Locations.Single(l => l.Id == locationWithCustomValue.Id).ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldNotBe(newValue);
    }

    [TestMethod]
    public void UpdateDefaultConfigurationParameter_UnknownParameter_ReturnsNull()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        trigger.UpdateDefaultConfigurationParameter("unknown", "any new value").ShouldBeNull();
    }

    [TestMethod]
    public void UpdateConfigurationParameterAtLocation_ValidData_ParameterUpdated()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        var location = trigger.AddLocation("a node", Guid.NewGuid());
        // initialized with default
        trigger.Locations.Single(l => l.Id == location.Id).ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(parameter.Value);

        const string newValue = "a new value";
        trigger.UpdateConfigurationParameterAtLocation(location.Id, parameter.Name, newValue);
        trigger.Locations.Single(l => l.Id == location.Id).ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(newValue);
        trigger.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(parameter.Value);
    }

    [TestMethod]
    public void ResetConfigurationParameterAtLocation_ValidData_ResetsConfigurationParameterAtLocationToDefault()
    {
        const string custom = "custom";
        const string node = "a node id";
        var map = Guid.NewGuid();
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var location = trigger.AddLocation(node, map, [new ConfigurationParameter(parameter.Name, parameter.Description, custom, parameter.Value)]);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(custom);

        var updatedParameter = trigger.ResetConfigurationParameterAtLocation(location.Id, parameter.Name);
        updatedParameter.ShouldNotBeNull();
        updatedParameter.Value.ShouldBe(parameter.Value);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(parameter.Value);
    }

    [TestMethod]
    public void ResetConfigurationParameterAtLocation_UnknownLocation_ThrowsTriggerLocationNotFoundException()
    {
        const string custom = "custom";
        const string node = "a node id";
        var map = Guid.NewGuid();
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var location = trigger.AddLocation(node, map, [new ConfigurationParameter(parameter.Name, parameter.Description, custom, parameter.Value)]);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(custom);

        Should.Throw<TriggerLocationNotFoundException>(() => trigger.ResetConfigurationParameterAtLocation("unknown".ToGuid(), parameter.Name));
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(custom);
    }

    [TestMethod]
    public void ResetConfigurationParameterAtLocation_UnknownParameter_ReturnsNullDoesNotResetParameter()
    {
        const string custom = "custom";
        const string node = "a node id";
        var map = Guid.NewGuid();
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var location = trigger.AddLocation(node, map, [new ConfigurationParameter(parameter.Name, parameter.Description, custom, parameter.Value)]);
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(custom);

        trigger.ResetConfigurationParameterAtLocation(location.Id, "unknown").ShouldBeNull();
        location.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe(custom);
    }
}