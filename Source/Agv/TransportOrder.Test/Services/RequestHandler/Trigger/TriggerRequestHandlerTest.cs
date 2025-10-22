using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using MassTransit;
using Shouldly;
using PolygonPoint = Gebhardt.Acc.TransportOrder.Domain.Models.Polygon.PolygonPoint;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Trigger;

[TestClass]
public class TriggerRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetTriggers_ValidData_RespondsGotTriggers()
    {
        List<TransportOrder.Domain.Models.Trigger.Trigger> triggers = [new(Guid.NewGuid(), "urn", "name", "version", "description", null, [])];
        TriggerRequestHandlerTestFixture<GetTriggers> fixture = new TriggerRequestHandlerTestFixture<GetTriggers>()
            .WithTriggers(triggers)
            .Build();

        Response<GotTriggers> response = await fixture.GetResponseAsync<GotTriggers>(new GetTriggers());
        response.Message.Triggers.ShouldBeEquivalentTo(triggers);
    }

    [TestMethod]
    public async Task Consume_GetTrigger_ValidData_RespondsGotTrigger()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);

        TriggerRequestHandlerTestFixture<GetTrigger> fixture = new TriggerRequestHandlerTestFixture<GetTrigger>()
            .WithTriggers([trigger])
            .Build();

        Response<GotTrigger> response = await fixture.GetResponseAsync<GotTrigger>(new GetTrigger(trigger.Id));
        response.Message.Trigger.ShouldBeEquivalentTo(trigger);
    }

    [TestMethod]
    public async Task Consume_GetTrigger_UnknownTrigger_ThrowsTriggerNotFoundException()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);

        TriggerRequestHandlerTestFixture<GetTrigger> fixture = new TriggerRequestHandlerTestFixture<GetTrigger>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () => await fixture.GetResponseAsync<GotTrigger>(new GetTrigger("unknown".ToGuid())));
    }

    [TestMethod]
    public async Task Consume_TryUpdateDefaultConfigurationParameter_ValidData_PublishesUpdateDefaultConfigurationParameterRespondsSuccess()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await fixture.GetResponseAsync<Success>(new TryUpdateDefaultConfigurationParameter(trigger.Id, parameter.Name, "new value"));
        await fixture.ShouldHavePublishedAsync<UpdateDefaultConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateDefaultConfigurationParameter_UnknownTrigger_ThrowsTriggerNotFoundException_DoesNotPublishUpdateDefaultConfigurationParameter()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () => await fixture.GetResponseAsync<Success>(new TryUpdateDefaultConfigurationParameter("unknown".ToGuid(), parameter.Name, "new value")));
        await fixture.ShouldNotHavePublishedAsync<UpdateDefaultConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateDefaultConfigurationParameter_ValidationFails_ThrowsTriggerConfigurationUpdateFailedException_DoesNotPublishUpdateDefaultConfigurationParameter()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "0", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerConfigurationUpdateFailedException>(async () => await fixture.GetResponseAsync<Success>(new TryUpdateDefaultConfigurationParameter(trigger.Id, parameter.Name, "new value is not number")));
        await fixture.ShouldNotHavePublishedAsync<UpdateDefaultConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateLocationConfigurationParameter_UnknownTrigger_ThrowsTriggerNotFoundException_DoesNotPublishUpdateLocationConfigurationParameter()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        trigger.AddLocation("nodeId".ToGraphNode(), null, null);
        TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryUpdateLocationConfigurationParameter("unknown".ToGuid(),
                trigger.Locations.First().Id,
                parameter.Name,
                "new value")));
        await fixture.ShouldNotHavePublishedAsync<UpdateLocationConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateLocationConfigurationParameter_UnknownConfigurationParameter_ThrowsTriggerConfigurationUpdateFailedException_DoesNotPublishUpdateLocationConfigurationParameter()
    {
        var parameter = new ConfigurationParameter("parameter name", "description", "value", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        trigger.AddLocation("nodeId".ToGraphNode(), null, null);
        TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerConfigurationUpdateFailedException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryUpdateLocationConfigurationParameter(trigger.Id,
                trigger.Locations.First().Id,
                "unknown",
                "new value")));
        await fixture.ShouldNotHavePublishedAsync<UpdateLocationConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateLocationConfigurationParameter_InvalidConfigurationParameter_ThrowsTriggerConfigurationUpdateFailedException_DoesNotPublishUpdateLocationConfigurationParameter()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "12", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        trigger.AddLocation("nodeId".ToGraphNode(), null, null);
        TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerConfigurationUpdateFailedException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryUpdateLocationConfigurationParameter(trigger.Id,
                trigger.Locations.First().Id,
                parameter.Name,
                "not a number")));
        await fixture.ShouldNotHavePublishedAsync<UpdateLocationConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateLocationConfigurationParameter_UnknownLocation_ThrowsTriggerLocationNotFoundException_DoesNotPublishUpdateLocationConfigurationParameter()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "12", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        trigger.AddLocation("nodeId".ToGraphNode(), null, null);
        TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerLocationNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryUpdateLocationConfigurationParameter(trigger.Id,
                "unknown".ToGuid(),
                parameter.Name,
                parameter.Value)));
        await fixture.ShouldNotHavePublishedAsync<UpdateLocationConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateLocationConfigurationParameter_ValidData_PublishesUpdateLocationConfigurationParameter()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "12", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        trigger.AddLocation("nodeId".ToGraphNode(), null, null);
        TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateLocationConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await fixture.GetResponseAsync<Success>(new TryUpdateLocationConfigurationParameter(trigger.Id,
            trigger.Locations.First().Id,
            parameter.Name,
            "42"));
        await fixture.ShouldHavePublishedAsync<UpdateLocationConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_UnknownConfigurationParameter_ThrowsTriggerAssignmentFailedException_DoesNotRequestAddTriggerLocation()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "0", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(true)
            .Build();
        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () =>
            await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id,
                "a node".ToGraphNode(),
                Guid.NewGuid(),
                [],
                Guid.NewGuid(),
                [
                    new ConfigurationParameter("unknown",
                        parameter.Description,
                        parameter.Value,
                        parameter.Validation)
                ])));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_InvalidConfigurationParameter_ThrowsTriggerAssignmentFailedException_DoesNotRequestAddTriggerLocation()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "0", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(true)
            .Build();
        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () =>
            await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id,
                "a node".ToGraphNode(),
                Guid.NewGuid(),
                [],
                Guid.NewGuid(),
                [
                    new ConfigurationParameter(parameter.Name,
                        parameter.Description,
                        "not a number",
                        parameter.Validation)
                ])));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_UnknownTrigger_ThrowsTriggerNotFoundException_DoesNotRequestAddTriggerLocation()
    {
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () =>
            await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation("unknown".ToGuid(),
                Guid.NewGuid().ToGraphNode(),
                null,
                [],
                Guid.NewGuid(),
                null)));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_MultipleLocationsGiven_ThrowsTriggerAssignmentFailedException_DoesNotRequestAddTriggerLocation()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(true)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () =>
            await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id,
                "a node".ToGraphNode(),
                Guid.NewGuid(),
                [],
                Guid.NewGuid(),
                null)));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_EdgeUnknown_ThrowsTriggerAssignmentFailedException_DoesNotRequestAddTriggerLocation()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(false)
            .WithMapExists(true)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () =>
            await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id,
                null,
                "unknown-edge".ToGuid(),
                [],
                Guid.NewGuid(),
                null)));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_NodeUnknown_ThrowsNodeNotFoundException_DoesNotRequestAddTriggerLocation()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(false)
            .WithMapExists(true)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () =>
            await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id,
                "unknown node".ToGraphNode(),
                null,
                [],
                Guid.NewGuid(),
                null)));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_MapGivenButUnknown_ThrowsTriggerAssignmentFailedException_DoesNotRequestAddTriggerLocation()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(false)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () =>
            await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id,
                null,
                Guid.NewGuid(),
                [],
                "unknown-map".ToGuid(),
                null)));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_ValidNode_PublishesAddTriggerLocationRespondsTriggerLocationAssigned()
    {
        var node = "a node".ToGraphNode();
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var location = new Location(node, null, Guid.NewGuid(), trigger.Id, []);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(true)
            .WithLocationResponse(location)
            .WithNode(node)
            .Build();

        Response<TriggerLocationAssigned> response = await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, location.Node!, null, [], location.MapId, null));
        response.Message.Location.ShouldBeEquivalentTo(location);
        await fixture.ShouldHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_ValidEdge_PublishesAddTriggerLocationRespondsTriggerLocationAssigned()
    {
        var edgeId = "an edge".ToGuid();
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var edgeLocation = new Location(null, edgeId, Guid.NewGuid(), trigger.Id, []);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(true)
            .WithLocationResponse(edgeLocation)
            .Build();

        Response<TriggerLocationAssigned> response = await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, null, edgeLocation.EdgeId!, [], edgeLocation.MapId, null));
        response.Message.Location.ShouldBeEquivalentTo(edgeLocation);
        await fixture.ShouldHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_ValidPolygon_PublishesAddTriggerLocationRespondsTriggerLocationAssigned()
    {
        ICollection<PolygonPoint> domainPolygon = [new(-8.900d, -10.000d, 0), new(-9.5d, -10.000d, 1), new(-8.900d, -11.000d, 2)];
        ICollection<Common.Domain.Models.PolygonPoint> triggerPolygon = [new(-8.900d, -10.000d), new(-9.5d, -10.000d), new(-8.900d, -11.000d)];

        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var polygonLocation = new Location(domainPolygon, Guid.NewGuid(), trigger.Id, []);

        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(false)
            .WithMapExists(true)
            .WithLocationResponse(polygonLocation)
            .Build();

        Response<TriggerLocationAssigned> response = await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, null, null, triggerPolygon, polygonLocation.MapId, null));
        response.Message.Location.ShouldBeEquivalentTo(polygonLocation);
        await fixture.ShouldHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_MapNotGiven_PublishesAddTriggerLocationRespondsTriggerLocationAssigned()
    {
        var node = "a node".ToGraphNode();
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var location = new Location(node, null, Guid.NewGuid(), trigger.Id, []);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(false)
            .WithLocationResponse(location)
            .WithNode(node)
            .Build();

        Response<TriggerLocationAssigned> response = await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, location.Node!, null, [], null, null));
        response.Message.Location.ShouldBeEquivalentTo(location);
        await fixture.ShouldHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_TriggerLocationAdditionThrowsException_ThrowsTriggerAssignmentFailedException()
    {
        var node = "a node".ToGraphNode();
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var location = new Location(node, null, Guid.NewGuid(), trigger.Id, []);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(true)
            .WithLocationResponse(null, "an exception")
            .WithNode(node)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () => await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, location.Node!, null, [], location.MapId, null)));
    }

    [TestMethod]
    public async Task Consume_TryResetLocationConfigurationParameter_ValidData_PublishesResetLocationConfigurationParameter()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "12", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        trigger.AddLocation("nodeId".ToGraphNode(), null, null);
        TriggerRequestHandlerTestFixture<TryResetLocationConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryResetLocationConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await fixture.GetResponseAsync<Success>(new TryResetLocationConfigurationParameter(trigger.Id, trigger.Locations.First().Id, parameter.Name));
        await fixture.ShouldHavePublishedAsync<ResetLocationConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryResetLocationConfigurationParameter_UnknownTrigger_ThrowsTriggerNotFoundException_DoesNotPublishResetLocationConfigurationParameter()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "12", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        trigger.AddLocation("node".ToGraphNode(), null, null);
        TriggerRequestHandlerTestFixture<TryResetLocationConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryResetLocationConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryResetLocationConfigurationParameter("unknown".ToGuid(),
                trigger.Locations.First().Id,
                parameter.Name)));
        await fixture.ShouldNotHavePublishedAsync<ResetLocationConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryResetLocationConfigurationParameter_UnknownTriggerLocation_ThrowsTriggerLocationNotFoundException_DoesNotPublishResetLocationConfigurationParameter()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "description", "12", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        trigger.AddLocation("nodeName".ToGraphNode(), null, null);
        TriggerRequestHandlerTestFixture<TryResetLocationConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryResetLocationConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerLocationNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryResetLocationConfigurationParameter(trigger.Id,
                "unknown".ToGuid(),
                parameter.Name)));
        await fixture.ShouldNotHavePublishedAsync<ResetLocationConfigurationParameter>();
    }
}