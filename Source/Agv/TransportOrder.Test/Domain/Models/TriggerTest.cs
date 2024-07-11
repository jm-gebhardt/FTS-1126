using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

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
        var configurationParameter = new ConfigurationParameter("name", "value", "validation regex");

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
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [new ConfigurationParameter("parameter name", "value", "validation regex")]);
       
        var location = trigger.AddLocation(node, map);
        
        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBeNull();
        location.Polygon.ShouldBeEmpty();
        location.NodeId.ShouldBe(node);
        location.MapId.ShouldBe(map);
    }

    [TestMethod]
    public void AddLocation_NodeId_InstanceMaximumExceeded_ThrowsTriggerInstanceCountException()
    {
        const int instances = 1;
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", instances, [new ConfigurationParameter("parameter name", "value", "validation regex")]);
        trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(instances);

        Should.Throw<TriggerInstanceCountException>(() =>trigger.AddLocation("a node", Guid.NewGuid()));
    }

    [TestMethod]
    public void AddLocation_EdgeId_LocationAdded()
    {
        var edge = Guid.NewGuid();
        var map = Guid.NewGuid();
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [new ConfigurationParameter("parameter name", "value", "validation regex")]);

        var location = trigger.AddLocation(edge, map);

        trigger.Locations.Count.ShouldBe(1);
        trigger.Locations.First().ShouldBe(location);
        location.EdgeId.ShouldBe(edge);
        location.Polygon.ShouldBeEmpty();
        location.NodeId.ShouldBeNull();
        location.MapId.ShouldBe(map);
    }

    [TestMethod]
    public void AddLocation_EdgeId_InstanceMaximumExceeded_ThrowsTriggerInstanceCountException()
    {
        const int instances = 1;
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", instances, [new ConfigurationParameter("parameter name", "value", "validation regex")]);
        trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(instances);

        Should.Throw<TriggerInstanceCountException>(() => trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid()));
    }

    [TestMethod]
    public void AddLocation_Polygon_LocationAdded()
    {
        List<PolygonPoint> polygon = [new PolygonPoint(42.0, 42.0, Guid.NewGuid())];
        var map = Guid.NewGuid();
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [new ConfigurationParameter("parameter name", "value", "validation regex")]);

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
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", instances, [new ConfigurationParameter("parameter name", "value", "validation regex")]);
        trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(instances);

        Should.Throw<TriggerInstanceCountException>(() => trigger.AddLocation([new PolygonPoint(42.0, 42.0, Guid.NewGuid())], Guid.NewGuid()));
    }

    [TestMethod]
    public void RemoveLocation_ValidData_LocationRemoved()
    {
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [new ConfigurationParameter("parameter name", "value", "validation regex")]);
        var location = trigger.AddLocation("a node", Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(1);

        trigger.RemoveLocation(location.Id);
        trigger.Locations.ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveLocation_LocationNotFound_ThrowsTriggerLocationNotFoundException()
    {
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", 12, [new ConfigurationParameter("parameter name", "value", "validation regex")]);

        Should.Throw<TriggerLocationNotFoundException>(() => trigger.RemoveLocation("unknown".ToGuid()));
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_NoValidation_ReturnsTrue()
    {
        var parameter = new ConfigurationParameter("parameter name", "value", string.Empty);
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
        var parameter = new ConfigurationParameter("parameter name", "0", isNumber);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "not a number").ShouldBeFalse();
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_ValidationIsNotValidRegex_ReturnsTrue()
    {
        var parameter = new ConfigurationParameter("parameter name", "0", "([");
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "any new value").ShouldBeTrue();
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_Validates_ReturnsTrue()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "0", isNumber);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "12").ShouldBeTrue();
    }

    [TestMethod]
    public void ValidateConfigurationParameterValue_EscapedSpecialBackslash_ReturnsTrue()
    {
        const string isNumber = "^-?\\d+$";
        var parameter = new ConfigurationParameter("parameter name", "0", isNumber);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.ValidateConfigurationParameterValue(parameter.Name, "12").ShouldBeTrue();
    }

    [TestMethod]
    public void UpdateConfigurationParameter_ValidData_UpdatesParameterValue()
    {
        var parameter = new ConfigurationParameter("parameter name", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        const string newValue = "a new value";

        var updatedParameter = trigger.UpdateConfigurationParameter(parameter.Name, newValue);
        
        updatedParameter.ShouldNotBeNull();
        updatedParameter.Name.ShouldBe(parameter.Name);
        updatedParameter.Validation.ShouldBe(parameter.Validation);
        updatedParameter.Value.ShouldBe(newValue);
    }

    [TestMethod]
    public void UpdateConfigurationParameter_UnknownParameter_ReturnsNull()
    {
        var parameter = new ConfigurationParameter("parameter name", "value", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        trigger.UpdateConfigurationParameter("unknown", "any new value").ShouldBeNull();
    }
}