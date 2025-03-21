using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<CreateTrigger>();

        fixture.UpdatedTrigger.DisplayName.ShouldBe(command.Properties.DisplayName);
        fixture.UpdatedTrigger.Description.ShouldBe(command.Properties.Description);
        fixture.UpdatedTrigger.Urn.ShouldBe(command.Properties.Urn);
        fixture.UpdatedTrigger.ConfigurationParameters.ShouldBeEquivalentTo(command.ConfigurationParameters);
    }

    [TestMethod]
    public async Task Consume_RemoveTriggerLocation_ValidData_TriggerLocationRemoved()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", "validation regex");
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var location = trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(1);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.TestHarness.Bus.Publish(new RemoveTriggerLocation(trigger.Id, location.Id));

        await fixture.ShouldHaveConsumedAsync<RemoveTriggerLocation>();
        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_UpdateDefaultConfigurationParameter_ValidData_PublishesDefaultTriggerConfigurationUpdated()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.TestHarness.Bus.Publish(new UpdateDefaultConfigurationParameter(trigger.Id, parameter.Name, "new value"));

        await fixture.ShouldHaveConsumedAsync<UpdateDefaultConfigurationParameter>();
        await fixture.ShouldHavePublishedAsync<DefaultTriggerConfigurationUpdated>();
        fixture.UpdatedTrigger.ConfigurationParameters.Single( p => p.Name == parameter.Name).Value.ShouldBe("new value");
    }

    [TestMethod]
    public async Task Consume_UpdateDefaultConfigurationParameter_UnknownParameter_DoesNotPublishTriggerOrUpdateConfiguration()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.TestHarness.Bus.Publish(new UpdateDefaultConfigurationParameter(trigger.Id, "unknown", "new value"));

        await fixture.ShouldHaveConsumedAsync<UpdateDefaultConfigurationParameter>();
        await fixture.ShouldNotHavePublishedAsync<DefaultTriggerConfigurationUpdated>();
        fixture.UpdatedTrigger.ShouldBeEquivalentTo(null!);
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_EdgeGiven_LocationAdded_TriggerAssignedToNodeNotPublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, null, Guid.NewGuid(),  [], Guid.NewGuid(), null);

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<AddTriggerLocation>();
        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.Count.ShouldBe(1);
        fixture.UpdatedTrigger.Locations.First().EdgeId.ShouldBe(command.EdgeId);

        await fixture.ShouldNotHavePublishedAsync<TriggerAssignedToNode>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_PolygonGiven_LocationAdded_TriggerAssignedToNodeNotPublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, null, null, [new PolygonPoint(12,12)], Guid.NewGuid(), null);

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<AddTriggerLocation>();
        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.Count.ShouldBe(1);
        fixture.UpdatedTrigger.Locations.First().Polygon.Count.ShouldBe(command.Polygon.Count);
 
        await fixture.ShouldHavePublishedAsync<TriggerAssignedToArea>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_NodeGiven_LocationAdded_TriggerAssignedToNodeNotPublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, "a node", null, [], Guid.NewGuid(), null);

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<AddTriggerLocation>();
        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.Count.ShouldBe(1);
        fixture.UpdatedTrigger.Locations.First().NodeId.ShouldBe(command.NodeId);

        await fixture.ShouldHavePublishedAsync<TriggerAssignedToNode>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }

    [TestMethod]
    public async Task Consume_UpdateLocationConfigurationParameter_UnknownTriggerLocation_NotUpdated()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.AddLocation("a node", null);
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.TestHarness.Bus.Publish(new UpdateLocationConfigurationParameter(trigger.Id, "unknown".ToGuid(), parameter.Name, "new value"));

        await fixture.ShouldHaveConsumedAsync<UpdateLocationConfigurationParameter>();
        fixture.UpdatedTrigger.ShouldBeEquivalentTo(null!);
    }

    [TestMethod]
    public async Task Consume_UpdateLocationConfigurationParameter_ValidData_TriggerUpdated()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        trigger.AddLocation("a node", null);
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        const string newValue = "new value";
        await fixture.TestHarness.Bus.Publish(new UpdateLocationConfigurationParameter(trigger.Id, trigger.Locations.First().Id, parameter.Name, newValue));

        await fixture.ShouldHaveConsumedAsync<UpdateLocationConfigurationParameter>();
        fixture.UpdatedTrigger.Id.ShouldBe(trigger.Id);
        fixture.UpdatedTrigger.Locations.Count.ShouldBe(1);
        fixture.UpdatedTrigger.Locations.First().ConfigurationParameters.First(c => c.Name == parameter.Name).Value.ShouldBe(newValue);
    }
}