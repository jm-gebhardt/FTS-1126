using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
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

        fixture.Trigger.DisplayName.ShouldBe(command.Properties.DisplayName);
        fixture.Trigger.Description.ShouldBe(command.Properties.Description);
        fixture.Trigger.Urn.ShouldBe(command.Properties.Urn);
        fixture.Trigger.ConfigurationParameters.ShouldBeEquivalentTo(command.ConfigurationParameters);
    }

    [TestMethod]
    public async Task Consume_RemoveTriggerLocation_ValidData_TriggerLocationRemoved()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [new ConfigurationParameter("parameter name", "value", "validation regex")]);

        var location = trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        trigger.Locations.Count.ShouldBe(1);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.TestHarness.Bus.Publish(new RemoveTriggerLocation(trigger.Id, location.Id));

        await fixture.ShouldHaveConsumedAsync<RemoveTriggerLocation>();
        fixture.Trigger.Id.ShouldBe(trigger.Id);
        fixture.Trigger.Locations.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_UpdateConfigurationParameter_ValidData_PublishesTriggerConfigurationUpdated()
    {
        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.TestHarness.Bus.Publish(new UpdateConfigurationParameter(trigger.Id, parameter.Name, "new value"));

        await fixture.ShouldHaveConsumedAsync<UpdateConfigurationParameter>();
        await fixture.ShouldHavePublishedAsync<TriggerConfigurationUpdated>();
        fixture.Trigger.ConfigurationParameters.Single( p => p.Name == parameter.Name).Value.ShouldBe("new value");
    }

    [TestMethod]
    public async Task Consume_UpdateConfigurationParameter_UnknownParameter_DoesNotPublishTriggerOrUpdateConfiguration()
    {
        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        await fixture.TestHarness.Bus.Publish(new UpdateConfigurationParameter(trigger.Id, "unknown", "new value"));

        await fixture.ShouldHaveConsumedAsync<UpdateConfigurationParameter>();
        await fixture.ShouldNotHavePublishedAsync<TriggerConfigurationUpdated>();
        fixture.Trigger.ShouldBeEquivalentTo(null!);
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_EdgeGiven_LocationAdded_TriggerAssignedToNodeNotPublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, Guid.NewGuid(), null, [], Guid.NewGuid());

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<AddTriggerLocation>();
        fixture.Trigger.Id.ShouldBe(trigger.Id);
        fixture.Trigger.Locations.Count.ShouldBe(1);
        fixture.Trigger.Locations.First().EdgeId.ShouldBe(command.EdgeId);

        await fixture.ShouldNotHavePublishedAsync<TriggerAssignedToNode>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_PolygonGiven_LocationAdded_TriggerAssignedToNodeNotPublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, null, null, [new PolygonPoint(12,12, Guid.NewGuid())], Guid.NewGuid());

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<AddTriggerLocation>();
        fixture.Trigger.Id.ShouldBe(trigger.Id);
        fixture.Trigger.Locations.Count.ShouldBe(1);
        fixture.Trigger.Locations.First().Polygon.Count.ShouldBe(command.Polygon.Count);

        await fixture.ShouldNotHavePublishedAsync<TriggerAssignedToNode>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }

    [TestMethod]
    public async Task Consume_AddTriggerLocation_NodeGiven_LocationAdded_TriggerAssignedToNodeNotPublished()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);
        trigger.Locations.ShouldBeEmpty();
        var fixture = new TriggerCommandHandlerTestFixture().WithTrigger(trigger);

        var command = new AddTriggerLocation(trigger.Id, null, "a node", [], Guid.NewGuid());

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<AddTriggerLocation>();
        fixture.Trigger.Id.ShouldBe(trigger.Id);
        fixture.Trigger.Locations.Count.ShouldBe(1);
        fixture.Trigger.Locations.First().NodeId.ShouldBe(command.NodeId);

        await fixture.ShouldHavePublishedAsync<TriggerAssignedToNode>();
        await fixture.ShouldHavePublishedAsync<TriggerLocationAdded>();
    }
}