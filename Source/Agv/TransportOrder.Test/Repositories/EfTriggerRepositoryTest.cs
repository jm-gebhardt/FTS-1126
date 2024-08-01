using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
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
        
        await fixture.Repository.Add(trigger);
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

    [TestMethod]
    public async Task GetTriggersAssignedToNode_WithNodeLocation_TriggersReturned()
    {
        const string nodeIdToUse = "WP-0010";
        const string otherNodeId = "WP-0015";

        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "4 trigger", "version", "description", null, [parameter])
        };
        triggers[0].AddLocation(nodeIdToUse, null);
        triggers[1].AddLocation(otherNodeId, null);
        triggers[2].AddLocation(nodeIdToUse, null);

        ITriggerRepository repo = fixture.Repository;
        await repo.Add(triggers[0]);
        await repo.Add(triggers[1]);
        await repo.Add(triggers[2]);
        await repo.Add(triggers[3]);

        ICollection<Guid> triggerIds = fixture.Repository.GetTriggersAssignedToNode(nodeIdToUse);
        triggerIds.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task GetTriggersAssignedToNode_WithNodeLocationNoTriggerAvailable_TriggersReturned()
    {
        const string nodeIdNotAvailable = "WP-0010";
        const string otherNodeId = "WP-0015";

        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "4 trigger", "version", "description", null, [parameter])
        };
        triggers[0].AddLocation(otherNodeId, null);
        triggers[1].AddLocation(otherNodeId, null);

        ITriggerRepository repo = fixture.Repository;
        await repo.Add(triggers[0]);
        await repo.Add(triggers[1]);
        await repo.Add(triggers[2]);
        await repo.Add(triggers[3]);

        ICollection<Guid> triggerIds = fixture.Repository.GetTriggersAssignedToNode(nodeIdNotAvailable);
        triggerIds.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetTriggersAssignedToNode_EmptyNodeId_EmptyTriggerListReturned()
    {
        const string nodeIdToUse = "";
        const string otherNodeId = "WP-0009";
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "4 trigger", "version", "description", null, [parameter])
        };
        triggers[1].AddLocation(otherNodeId, null);
        triggers[2].AddLocation(otherNodeId, null);
        triggers[3].AddLocation(otherNodeId, null);

        ITriggerRepository repo = fixture.Repository;
        await repo.Add(triggers[0]);
        await repo.Add(triggers[1]);
        await repo.Add(triggers[2]);
        await repo.Add(triggers[3]);

        ICollection<Guid> triggerIds = fixture.Repository.GetTriggersAssignedToNode(nodeIdToUse);
        triggerIds.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task LoadTriggerToLocationDictionary_WithNodeLocation_TriggersReturned()
    {
        const string nodeIdToUse = "WP-0010";
        const string otherNodeId = "WP-0015";

        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "4 trigger", "version", "description", null, [parameter])
        };
        triggers[0].AddLocation(nodeIdToUse, null);
        triggers[1].AddLocation(otherNodeId, null);
        triggers[2].AddLocation(nodeIdToUse, null);

        //await fixture.AddRangeAsync(triggers);
        ITriggerRepository repo = fixture.Repository;
        await repo.Add(triggers[0]);
        await repo.Add(triggers[1]);
        await repo.Add(triggers[2]);
        await repo.Add(triggers[3]);
        await repo.LoadTriggerToLocationDictionary();

        ICollection<Guid> triggerIds = fixture.Repository.GetTriggersAssignedToNode(nodeIdToUse);
        triggerIds.Count.ShouldBe(2);
    }
}