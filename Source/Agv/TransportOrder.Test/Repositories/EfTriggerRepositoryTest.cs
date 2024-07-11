using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfTriggerRepositoryTest
{
    [TestMethod]
    public async Task Update_WithAddedLocation_TriggerLocationAdded()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        
        await fixture.AddAsync(trigger);
        var addedTrigger = await fixture.Repository.GetById(trigger.Id);
        addedTrigger.ShouldBeEquivalentTo(trigger);
        addedTrigger.Locations.ShouldBeEmpty();

        trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        await fixture.Repository.Update(trigger);
        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Update_WithRemovedLocation_TriggerLocationRemoved()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        //Add trigger with location
        await fixture.AddAsync(trigger);
        var location = trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        await fixture.Repository.Update(trigger);
        
        // Remove location during update
        trigger.RemoveLocation(location.Id);
        trigger.Locations.ShouldBeEmpty();
        await fixture.Repository.Update(trigger);
       
        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Update_WithUpdatedConfigurationParameter_ParameterUpdated()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        await fixture.AddAsync(trigger);

        const string updatedValue = "new";
        trigger.UpdateConfigurationParameter(parameter.Name, updatedValue);
        await fixture.Repository.Update(trigger);

        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.ConfigurationParameters.Single(c => c.Name == parameter.Name).Value.ShouldBe(updatedValue);
    }
}