using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Trigger;

[TestClass]
public class TriggerCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateTrigger_ValidData_TriggerCreated()
    {
        var fixture = new TriggerCommandHandlerTestFixture();

        var command = new CreateTrigger(new TriggerProperties("urn", "name", "description"), []);

        await fixture.PublishMessageAsync(command);

        fixture.UpdatedTrigger.DisplayName.ShouldBe(command.Properties.DisplayName);
        fixture.UpdatedTrigger.Description.ShouldBe(command.Properties.Description);
        fixture.UpdatedTrigger.Urn.ShouldBe(command.Properties.Urn);
        fixture.UpdatedTrigger.ConfigurationParameters.ShouldBeEquivalentTo(command.ConfigurationParameters);

        await fixture.ShouldHavePublishedAsync<TriggerChanged>();
    }

    [TestMethod]
    public async Task Consume_TriggerUnchanged_ValidData_DoesNotPublishTriggerChanged()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var properties = new TriggerProperties("urn", "a trigger", "description", "version", 1);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(properties.Id, properties.Urn, properties.DisplayName, properties.Version, properties.Description, properties.InstanceMaximum, [parameter]);
        var fixture = new TriggerCommandHandlerTestFixture()
            .WithTrigger(trigger);

        var command = new CreateTrigger(properties, trigger.ConfigurationParameters);

        await fixture.PublishMessageAsync(command);

        fixture.UpdatedTrigger.DisplayName.ShouldBe(command.Properties.DisplayName);
        fixture.UpdatedTrigger.Description.ShouldBe(command.Properties.Description);
        fixture.UpdatedTrigger.Urn.ShouldBe(command.Properties.Urn);
        fixture.UpdatedTrigger.ConfigurationParameters.ShouldBeEquivalentTo(command.ConfigurationParameters);

        await fixture.ShouldNotHavePublishedAsync<TriggerChanged>();
    }

    [TestMethod]
    public async Task Consume_TriggerChanged_ValidData_PublishesTriggerChanged()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var properties = new TriggerProperties("urn", "a trigger", "description", "version", 1);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(properties.Id, properties.Urn, properties.DisplayName, properties.Version, properties.Description, properties.InstanceMaximum, [parameter]);
        var fixture = new TriggerCommandHandlerTestFixture()
            .WithTrigger(trigger);

        var command = new CreateTrigger(new TriggerProperties(properties.Urn, properties.DisplayName, "new description", properties.Version, properties.InstanceMaximum), trigger.ConfigurationParameters);

        await fixture.PublishMessageAsync(command);

        fixture.UpdatedTrigger.DisplayName.ShouldBe(command.Properties.DisplayName);
        fixture.UpdatedTrigger.Description.ShouldBe(command.Properties.Description);
        fixture.UpdatedTrigger.Urn.ShouldBe(command.Properties.Urn);
        fixture.UpdatedTrigger.ConfigurationParameters.ShouldBeEquivalentTo(command.ConfigurationParameters);

        await fixture.ShouldHavePublishedAsync<TriggerChanged>();
    }

    [TestMethod]
    public async Task Consume_RemoveTriggerLocation_ValidData_TriggerLocationRemoved()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", "validation regex");
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var location = trigger.AddLocation(Guid.NewGuid().ToGraphNode(), null, Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(1);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.PublishMessageAsync(new RemoveTriggerLocation(trigger.Id, location.Id));

        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.ShouldBeEmpty();
        await fixture.ShouldHavePublishedAsync<TriggerChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateDefaultConfigurationParameter_ValidData_PublishesDefaultTriggerConfigurationUpdated()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.PublishMessageAsync(new UpdateDefaultConfigurationParameter(trigger.Id, parameter.Name, "new value"));

        await fixture.ShouldHavePublishedAsync<DefaultTriggerConfigurationUpdated>();
        fixture.UpdatedTrigger.ConfigurationParameters.Single(p => p.Name == parameter.Name).Value.ShouldBe("new value");
        await fixture.ShouldHavePublishedAsync<TriggerChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateDefaultConfigurationParameter_UnknownParameter_DoesNotPublishTriggerOrUpdateConfiguration()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.PublishMessageAsync(new UpdateDefaultConfigurationParameter(trigger.Id, "unknown", "new value"));

        await fixture.ShouldNotHavePublishedAsync<DefaultTriggerConfigurationUpdated>();
        await fixture.ShouldNotHavePublishedAsync<TriggerChanged>();
        fixture.UpdatedTrigger.ShouldBeEquivalentTo(null!);
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_EdgeGiven_LocationAdded_TriggerAssignedToNodeNotPublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, null, Guid.NewGuid(), [], Guid.NewGuid(), null);

        await fixture.PublishMessageAsync(command);

        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.Count.ShouldBe(1);
        fixture.UpdatedTrigger.Locations.First().EdgeId.ShouldBe(command.EdgeId);

        await fixture.ShouldHavePublishedAsync<TriggerChanged>();
        await fixture.ShouldNotHavePublishedAsync<TriggerAssignedToNode>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_PolygonGiven_LocationAdded_TriggerAssignedToNodeNotPublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, null, null, [new PolygonPoint(12, 12)], Guid.NewGuid(), null);

        await fixture.PublishMessageAsync(command);

        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.Count.ShouldBe(1);
        fixture.UpdatedTrigger.Locations.First().Polygon.Count.ShouldBe(command.Polygon.Count);

        await fixture.ShouldHavePublishedAsync<TriggerChanged>();
        await fixture.ShouldHavePublishedAsync<TriggerAssignedToArea>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_NodeGiven_LocationAdded_TriggerAssignedToNodePublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, "a node".ToGraphNode(), null, [], Guid.NewGuid(), null);

        await fixture.PublishMessageAsync(command);

        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.Count.ShouldBe(1);
        fixture.UpdatedTrigger.Locations.First().Node.ShouldBe(command.Node);

        await fixture.ShouldHavePublishedAsync<TriggerChanged>();
        await fixture.ShouldHavePublishedAsync<TriggerAssignedToNode>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }

    [TestMethod]
    public async Task Consume_UpdateLocationConfigurationParameter_UnknownTriggerLocation_NotUpdated()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.AddLocation("a node".ToGraphNode(), null, null);
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.PublishMessageAsync(new UpdateLocationConfigurationParameter(trigger.Id, "unknown".ToGuid(), parameter.Name, "new value"));

        await fixture.ShouldNotHavePublishedAsync<TriggerChanged>();
        fixture.UpdatedTrigger.ShouldBeEquivalentTo(null!);
    }

    [TestMethod]
    public async Task Consume_UpdateLocationConfigurationParameter_ValidData_TriggerUpdated()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.AddLocation("a node".ToGraphNode(), null, null);
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        const string newValue = "new value";
        await fixture.PublishMessageAsync(new UpdateLocationConfigurationParameter(trigger.Id, trigger.Locations.First().Id, parameter.Name, newValue));

        await fixture.ShouldHavePublishedAsync<TriggerChanged>();
        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.Count.ShouldBe(1);
        fixture.UpdatedTrigger.Locations.First().ConfigurationParameters.First(c => c.Name == parameter.Name).Value.ShouldBe(newValue);
    }
}