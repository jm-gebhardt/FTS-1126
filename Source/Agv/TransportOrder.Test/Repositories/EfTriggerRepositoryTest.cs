using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
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

        trigger.AddLocation(null, Guid.NewGuid(), Guid.NewGuid());
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

        trigger.AddLocation([new PolygonPoint(42.0, 42.0, 0, Guid.NewGuid())], null, []);
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
        var location = trigger.AddLocation(Guid.NewGuid().ToGraphNode(), null, Guid.NewGuid());
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

        var nodeToUse = "WP-0080".ToGraphNode();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, [parameter]);

        //Add trigger with location
        await fixture.AddAsync(trigger);
        var location = trigger.AddLocation(nodeToUse, null, null);
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
        triggerToUpdate.Locations.First().Agvs = new List<Guid>() { Guid.NewGuid() };
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
    public async Task UpdateLocation_WithUpdatedConfigurationParameter_ParameterUpdated()
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
        var addedLocation = trigger.AddLocation(polygon, null, [], [Guid.NewGuid()]);
        await fixture.Repository.Update(trigger, true);

        var triggerToUpdate = await fixture.Repository.GetById(trigger.Id);
        // Update location 
        triggerToUpdate.Locations.ShouldNotBeEmpty();
        await fixture.Repository.Update(triggerToUpdate, true);

        const string updatedValue = "1";
        trigger.UpdateConfigurationParameterAtLocation(addedLocation.Id, "parameter name", updatedValue);
        await fixture.Repository.Update(trigger, true);

        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.First(l => l.Id == addedLocation.Id).ConfigurationParameters.Single(c => c.Name == parameter.Name).Value.ShouldBe(updatedValue);
    }

    [TestMethod]
    public async Task UpdateMultipleLocations_WithUpdatedConfigurationParameter_ParameterUpdated()
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

        ICollection<PolygonPoint> polygon2 =
        [
            new(2, 9, 0),
            new(2, 7, 1),
            new(2, 6, 2),
            new(3, 5, 3)
        ];

        //Add trigger with two locations
        await fixture.AddAsync(trigger);
        var addedLocation1 = trigger.AddLocation(polygon, null, [], [Guid.NewGuid()]);
        var addedLocation2 = trigger.AddLocation(polygon2, null, [], [Guid.NewGuid()]);
        await fixture.Repository.Update(trigger, true);

        var triggerToUpdate = await fixture.Repository.GetById(trigger.Id);
        // Update location 
        triggerToUpdate.Locations.ShouldNotBeEmpty();
        await fixture.Repository.Update(triggerToUpdate, true);

        const string updatedValue1 = "1";
        const string updatedValue2 = "5";
        trigger.UpdateConfigurationParameterAtLocation(addedLocation1.Id, "parameter name", updatedValue1);
        trigger.UpdateConfigurationParameterAtLocation(addedLocation2.Id, "parameter name", updatedValue2);
        await fixture.Repository.Update(trigger, true);

        var updatedTrigger = await fixture.Repository.GetById(trigger.Id);
        updatedTrigger.ShouldBeEquivalentTo(trigger);
        updatedTrigger.Locations.First(l => l.Id == addedLocation1.Id).ConfigurationParameters.Single(c => c.Name == parameter.Name).Value.ShouldBe(updatedValue1);
        updatedTrigger.Locations.First(l => l.Id == addedLocation2.Id).ConfigurationParameters.Single(c => c.Name == parameter.Name).Value.ShouldBe(updatedValue2);
    }

    [TestMethod]
    public async Task GetTriggersAssignedToNode_WithNodeLocation_TriggersReturned()
    {
        var nodeToUse = "WP-0010".ToGraphNode();
        var otherNode = "WP-0015".ToGraphNode();

        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "4 trigger", "version", "description", null, [parameter])
        };
        triggers[0].AddLocation(nodeToUse, null, null);
        triggers[1].AddLocation(otherNode, null, null);
        triggers[2].AddLocation(nodeToUse, null, null);

        ITriggerRepository repo = fixture.Repository;
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);
        await repo.AddOrUpdate(triggers[2]);
        await repo.AddOrUpdate(triggers[3]);

        ICollection<Location> triggerLocations = fixture.Repository.GetNodeTriggerLocations(nodeToUse.Id);
        triggerLocations.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task GetTriggersAssignedToNode_WithNodeLocationNoTriggerAvailable_TriggersReturned()
    {
        var nodeIdNotAvailable = "WP-0010".ToGuid();
        var otherNode = "WP-0015".ToGraphNode();

        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "4 trigger", "version", "description", null, [parameter])
        };
        triggers[0].AddLocation(otherNode, null, null);
        triggers[1].AddLocation(otherNode, null, null);

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
        var nodeIdToUse = "".ToGuid();
        var otherNode = "WP-0009".ToGraphNode();
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "4 trigger", "version", "description", null, [parameter])
        };
        triggers[1].AddLocation(otherNode, null, null);
        triggers[2].AddLocation(otherNode, null, null);
        triggers[3].AddLocation(otherNode, null, null);

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
        var nodeToUse = "WP-0010".ToGraphNode();
        var otherNode = "WP-0015".ToGraphNode();

        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "3 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "4 trigger", "version", "description", null, [parameter])
        };
        triggers[0].AddLocation(nodeToUse, null, null);
        triggers[1].AddLocation(otherNode, null, null);
        triggers[2].AddLocation(nodeToUse, null, null);

        ITriggerRepository repo = fixture.Repository;
        await repo.AddOrUpdate(triggers[0]);
        await repo.AddOrUpdate(triggers[1]);
        await repo.AddOrUpdate(triggers[2]);
        await repo.AddOrUpdate(triggers[3]);
        await repo.LoadTriggerToLocationDictionary();

        ICollection<Location> triggerLocations = fixture.Repository.GetNodeTriggerLocations(nodeToUse.Id);
        triggerLocations.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task UpdateTriggerRepository_WithUpdatedConfigurationParameter_UpdatedTriggersReturned()
    {
        var nodeToUse = "WP-0010".ToGraphNode();
        const string updatedValue = "new value";
        await using var fixture = new EfTriggerRepositoryTestFixture();

        var parameter = new ConfigurationParameter("parameter name", "description", "0", string.Empty);
        var triggers = new List<Trigger>()
        {
            new(Guid.NewGuid(), "urn", "1 trigger", "version", "description", null, [parameter]),
            new(Guid.NewGuid(), "urn", "2 trigger", "version", "description", null, [parameter])
        };
        triggers[0].AddLocation(nodeToUse, null, null);
        triggers[1].AddLocation(nodeToUse, null, null);

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

        ICollection<PolygonTriggerResult> result = await fixture.Repository.GetPolygonTriggerLocations(agvId, agvPosition);
        List<Location> locations = result.Where(l => l.Action == PolygonTriggerAction.Entered).Select(l => l.Location).ToList();
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

        ICollection<PolygonTriggerResult> result = await fixture.Repository.GetPolygonTriggerLocations(agvId, agvPosition);
        List<Location> locations = result.Where(l => l.Action == PolygonTriggerAction.Entered).Select(l => l.Location).ToList();
        locations.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetPolygonLocationsWithTriggerToCallIgnoringMultiTrigger_AgvOutsidePolygonLocation_NoTriggersReturned()
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

        var agvPosition = new AgvPosition(true, 3f, 3f, 0, 0);

        ICollection<Location> locations = fixture.Repository.GetPolygonTriggerLocations(agvPosition);
        locations.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetPolygonLocationsWithTriggerToCallIgnoringMultiTrigger_AgvInsidePolygonLocation_TriggersReturned()
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

        var agvPosition = new AgvPosition(true, 1.5f, 1.5f, 0, 0);

        ICollection<Location> locations = fixture.Repository.GetPolygonTriggerLocations(agvPosition);
        locations.Count.ShouldBe(1);
        locations.First().TriggerId.ShouldBe(triggers[2].Id);
    }
}