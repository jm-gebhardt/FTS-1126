using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using PolygonPoint = Gebhardt.Acc.TransportOrder.Domain.Models.Polygon.PolygonPoint;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfTriggerRepositoryTest
{
    [TestMethod]
    public async Task Update_WithAddedEdgeLocation_TriggerEdgeLocationAdded()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        
        await fixture.Repository.AddOrUpdate(trigger, true);
        var addedTrigger = await fixture.Repository.GetById(trigger.Id);
        addedTrigger.ShouldBeEquivalentTo(trigger);
        addedTrigger.Locations.ShouldBeEmpty();

        trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        await fixture.Repository.Update(trigger, true);
        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Update_WithAddedPolygonLocation_TriggerPolygonLocationAdded()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        await fixture.Repository.AddOrUpdate(trigger, true);
        var addedTrigger = await fixture.Repository.GetById(trigger.Id);
        addedTrigger.ShouldBeEquivalentTo(trigger);
        addedTrigger.Locations.ShouldBeEmpty();

        trigger.AddLocation([new PolygonPoint(42.0, 42.0, 0, Guid.NewGuid())] , null, []);
        await fixture.Repository.Update(trigger, true);
        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Update_WithRemovedEdgeLocation_TriggerLocationRemoved()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        //Add trigger with location
        await fixture.AddAsync(trigger);
        var location = trigger.AddLocation(Guid.NewGuid(), Guid.NewGuid());
        await fixture.Repository.Update(trigger, true);
        
        // Remove location during update
        trigger.RemoveLocation(location.Id);
        trigger.Locations.ShouldBeEmpty();
        await fixture.Repository.Update(trigger, true);
       
        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Update_WithRemovedNodeLocation_TriggerLocationRemoved()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        const string nodeIdToUse = "WP-0080";

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        //Add trigger with location
        await fixture.AddAsync(trigger);
        var location = trigger.AddLocation(nodeIdToUse, null);
        await fixture.Repository.Update(trigger, true);

        // Remove location during update
        trigger.RemoveLocation(location.Id);
        trigger.Locations.ShouldBeEmpty();
        await fixture.Repository.Update(trigger, true);

        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Update_WithRemovedPolygonLocation_TriggerLocationRemoved()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        ICollection<PolygonPoint> polygon =
        [
            new(1, 1, 0),
            new(1, 2, 1),
            new(2, 1, 2),
            new(2, 2, 3)
        ];

        //Add trigger with location
        await fixture.AddAsync(trigger);
        var location = trigger.AddLocation(polygon, null, []);
        await fixture.Repository.Update(trigger, true);

        // Remove location during update
        trigger.RemoveLocation(location.Id);
        trigger.Locations.ShouldBeEmpty();
        await fixture.Repository.Update(trigger, true);

        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Update_WithUpdatedLocation_TriggerLocationUpdated()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        ICollection<PolygonPoint> polygon =
        [
            new(1, 1, 0),
            new(1, 2, 1),
            new(2, 1, 2),
            new(2, 2, 3)
        ];

        //Add trigger with location
        await fixture.AddAsync(trigger);
        trigger.AddLocation(polygon, null, []);
        await fixture.Repository.Update(trigger, true);

        var triggerToUpdate = await fixture.Repository.GetById(trigger.Id);
        // Update location 
        triggerToUpdate.Locations.First().Agvs = new List<Guid>(){Guid.NewGuid()};
        triggerToUpdate.Locations.ShouldNotBeEmpty();
        await fixture.Repository.Update(triggerToUpdate, true);

        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(triggerToUpdate);
        updatedTrigger.Locations.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task Update_WithUnchangedLocation_TriggerLocationNotUpdated()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        ICollection<PolygonPoint> polygon =
        [
            new(1, 1, 0),
            new(1, 2, 1),
            new(2, 1, 2),
            new(2, 2, 3)
        ];

        //Add trigger with location
        await fixture.AddAsync(trigger);
        trigger.AddLocation(polygon, null, [], [Guid.NewGuid()]);
        await fixture.Repository.Update(trigger, true);

        var triggerToUpdate = await fixture.Repository.GetById(trigger.Id);
        // Update location 
        triggerToUpdate.Locations.ShouldNotBeEmpty();
        await fixture.Repository.Update(triggerToUpdate, true);

        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(triggerToUpdate);
        updatedTrigger.Locations.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task Update_WithUpdatedConfigurationParameter_ParameterUpdated()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        await fixture.AddAsync(trigger);

        const string updatedValue = "new";
        trigger.UpdateDefaultConfigurationParameter(parameter.Name, updatedValue);
        await fixture.Repository.Update(trigger, true);

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

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
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
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);
        await repo.AddOrUpdate(triggers[2]);
        await repo.AddOrUpdate(triggers[3]);

        ICollection<Location> triggerLocations = fixture.Repository.GetNodeTriggerLocations(nodeIdToUse);
        triggerLocations.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task GetTriggersAssignedToNode_WithNodeLocationNoTriggerAvailable_TriggersReturned()
    {
        const string nodeIdNotAvailable = "WP-0010";
        const string otherNodeId = "WP-0015";

        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
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
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);
        await repo.AddOrUpdate(triggers[2]);
        await repo.AddOrUpdate(triggers[3]);

        ICollection<Location> triggerLocations = fixture.Repository.GetNodeTriggerLocations(nodeIdNotAvailable);
        triggerLocations.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetTriggersAssignedToNode_EmptyNodeId_EmptyTriggerListReturned()
    {
        const string nodeIdToUse = "";
        const string otherNodeId = "WP-0009";
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
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
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);
        await repo.AddOrUpdate(triggers[2]);
        await repo.AddOrUpdate(triggers[3]);

        ICollection<Location> triggerLocations = fixture.Repository.GetNodeTriggerLocations(nodeIdToUse);
        triggerLocations.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task LoadTriggerToLocationDictionary_WithNodeLocation_TriggersReturned()
    {
        const string nodeIdToUse = "WP-0010";
        const string otherNodeId = "WP-0015";

        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
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
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);
        await repo.AddOrUpdate(triggers[2]);
        await repo.AddOrUpdate(triggers[3]);
        await repo.LoadTriggerToLocationDictionary();

        ICollection<Location> triggerLocations = fixture.Repository.GetNodeTriggerLocations(nodeIdToUse);
        triggerLocations.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task UpdateTriggerRepository_WithUpdatedConfigurationParameter_UpdatedTriggersReturned()
    {
        const string nodeIdToUse = "WP-0010";
        const string updatedValue = "new value";
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter])
        };
        triggers[0].AddLocation(nodeIdToUse, null);
        triggers[1].AddLocation(nodeIdToUse, null);

        ITriggerRepository repo = fixture.Repository;
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);

        var updatedTrigger = await repo.GetById(triggers[0].Id);
        updatedTrigger.ConfigurationParameters.First().Value = updatedValue;
        await repo.AddOrUpdate(updatedTrigger);

        ICollection<Trigger> updatedTriggers = await repo.GetTriggers();
        updatedTriggers.Count.ShouldBe(2);
        updatedTriggers.First(l => l.Id == triggers[0].Id).ConfigurationParameters.First().Value.ShouldBe(updatedValue);
    }

    [TestMethod]
    public async Task GetById_WithInvalidTriggerId_TriggerNotFoundExceptionThrown()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);
        await fixture.AddAsync(trigger);

        await Should.ThrowAsync<TriggerNotFoundException>(fixture.Repository.GetById("unknown".ToGuid()));
    }

    [TestMethod]
    public async Task Update_WithInvalidTrigger_TriggerNotFoundExceptionThrown()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

         await Should.ThrowAsync<TriggerNotFoundException>(fixture.Repository.Update(trigger, true));
    }

    [TestMethod]
    public async Task GetPolygonLocationsWithTriggerToCall_AgvInsidePolygonLocation_TriggersReturned()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter])
        };

        ICollection<PolygonPoint> polygon =
        [
            new(1, 1, 0),
            new(1, 2, 1),
            new(2, 1, 2),
            new(2, 2, 3)
        ];

        triggers[2].AddLocation(polygon, null, []);

        ITriggerRepository repo = fixture.Repository;
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);
        await repo.AddOrUpdate(triggers[2]);
        var agvId = Guid.NewGuid();
        var agvPosition = new AgvPosition(true, 1.5f, 1.5f, 0, 0);

        ICollection<Location> locations = await fixture.Repository.GetPolygonTriggerLocations(agvId, agvPosition);
        locations.Count.ShouldBe(1);
        locations.First().TriggerId.ShouldBe(triggers[2].Id);
    }

    [TestMethod]
    public async Task GetPolygonLocationsWithTriggerToCall_AgvOutsidePolygonLocation_NoTriggersReturned()
    {
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter])
        };

        ICollection<PolygonPoint> polygon =
        [
            new(1, 1, 0),
            new(1, 2, 1),
            new(2, 1, 2),
            new(2, 2, 3)
        ];

        triggers[2].AddLocation(polygon, null, []);

        ITriggerRepository repo = fixture.Repository;
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);
        await repo.AddOrUpdate(triggers[2]);
        var agvId = Guid.NewGuid();
        var agvPosition = new AgvPosition(true, 3f, 3f, 0, 0);

        ICollection<Location> locations = await fixture.Repository.GetPolygonTriggerLocations(agvId, agvPosition);
        locations.Count.ShouldBe(0);
    }
}