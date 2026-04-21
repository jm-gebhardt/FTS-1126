using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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
        List<TransportOrder.Domain.Models.Trigger.Trigger> triggers = [TriggerGenerator.CreateTrigger()];
        TriggerRequestHandlerTestFixture<GetTriggers> fixture = new TriggerRequestHandlerTestFixture<GetTriggers>()
            .WithTriggers(triggers)
            .Build();

        Response<GotTriggers> response = await fixture.GetResponseAsync<GotTriggers>(new GetTriggers());
        response.Message.Triggers.ShouldBeEquivalentTo(triggers);
    }

    [TestMethod]
    public async Task Consume_GetTrigger_ValidData_RespondsGotTrigger()
    {
        var trigger = TriggerGenerator.CreateTrigger();

        TriggerRequestHandlerTestFixture<GetTrigger> fixture = new TriggerRequestHandlerTestFixture<GetTrigger>()
            .WithTriggers([trigger])
            .Build();

        Response<GotTrigger> response = await fixture.GetResponseAsync<GotTrigger>(new GetTrigger(trigger.Id));
        response.Message.Trigger.ShouldBeEquivalentTo(trigger);
    }

    [TestMethod]
    public async Task Consume_GetTrigger_UnknownTrigger_ThrowsTriggerNotFoundException()
    {
        var trigger = TriggerGenerator.CreateTrigger();

        TriggerRequestHandlerTestFixture<GetTrigger> fixture = new TriggerRequestHandlerTestFixture<GetTrigger>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () => await fixture.GetResponseAsync<GotTrigger>(new GetTrigger("unknown".ToGuid())));
    }

    [TestMethod]
    public async Task Consume_TryUpdateDefaultConfigurationParameter_ValidData_PublishesUpdateDefaultConfigurationParameterRespondsSuccess()
    {
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "value", string.Empty);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
        TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await fixture.GetResponseAsync<Success>(new TryUpdateDefaultConfigurationParameter(trigger.Id, parameter.Name, "new value"));
        await fixture.ShouldHavePublishedAsync<UpdateDefaultConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateDefaultConfigurationParameter_UnknownTrigger_ThrowsTriggerNotFoundException_DoesNotPublishUpdateDefaultConfigurationParameter()
    {
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "value", string.Empty);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "0", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
        TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateDefaultConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerConfigurationUpdateFailedException>(async () => await fixture.GetResponseAsync<Success>(new TryUpdateDefaultConfigurationParameter(trigger.Id, parameter.Name, "new value is not number")));
        await fixture.ShouldNotHavePublishedAsync<UpdateDefaultConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateLocationConfigurationParameter_UnknownTrigger_ThrowsTriggerNotFoundException_DoesNotPublishUpdateLocationConfigurationParameter()
    {
        var parameter = TriggerGenerator.CreateParameter();
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
        var trigger = TriggerGenerator.CreateTrigger();
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "12", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "12", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "12", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "0", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
                    TriggerGenerator.CreateParameter("unknown",
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "0", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
                    TriggerGenerator.CreateParameter(parameter.Name,
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
        var trigger = TriggerGenerator.CreateTrigger();
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
        var trigger = TriggerGenerator.CreateTrigger();
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
    public async Task Consume_AssignTriggerToLocation_NodeUnknown_ThrowsValidationException_DoesNotRequestAddTriggerLocation()
    {
        var trigger = TriggerGenerator.CreateTrigger();
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(false)
            .WithMapExists(true)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ValidationException>(async () =>
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
        var trigger = TriggerGenerator.CreateTrigger();
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
        var trigger = TriggerGenerator.CreateTrigger();
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
        var trigger = TriggerGenerator.CreateTrigger();
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

        var trigger = TriggerGenerator.CreateTrigger();
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
        var trigger = TriggerGenerator.CreateTrigger();
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
        var trigger = TriggerGenerator.CreateTrigger();
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "12", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "12", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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
        var parameter = TriggerGenerator.CreateParameter("parameter name", "description", "12", isNumber);
        var trigger = TriggerGenerator.CreateTrigger(parameters: [parameter]);
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

    [TestMethod]
    public async Task Consume_TryMarkTriggerForDeletion_ValidData_PublishesMarkTriggerForDeletion()
    {
        var trigger = TriggerGenerator.CreateTrigger();
        TriggerRequestHandlerTestFixture<TryMarkTriggerForDeletion> fixture = new TriggerRequestHandlerTestFixture<TryMarkTriggerForDeletion>()
            .WithTriggers([trigger])
            .Build();

        await fixture.GetResponseAsync<Success>(new TryMarkTriggerForDeletion(trigger.Id));
        await fixture.ShouldHavePublishedAsync<MarkTriggerForDeletion>();
    }

    [TestMethod]
    public async Task Consume_TryMarkTriggerForDeletion_UnknownTrigger_ThrowsTriggerNotFoundException()
    {
        var trigger = TriggerGenerator.CreateTrigger();
        TriggerRequestHandlerTestFixture<TryMarkTriggerForDeletion> fixture = new TriggerRequestHandlerTestFixture<TryMarkTriggerForDeletion>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryMarkTriggerForDeletion("unknown".ToGuid())));
        await fixture.ShouldNotHavePublishedAsync<MarkTriggerForDeletion>();
    }

    [TestMethod]
    public async Task Consume_GetDeletedTriggers_ValidData_RespondsGotDeletedTriggers()
    {
        List<TransportOrder.Domain.Models.Trigger.Trigger> deletedTriggers = [TriggerGenerator.CreateTrigger(isDeleted: true)];
        TriggerRequestHandlerTestFixture<GetDeletedTriggers> fixture = new TriggerRequestHandlerTestFixture<GetDeletedTriggers>()
            .WithDeletedTriggers(deletedTriggers)
            .Build();

        Response<GotDeletedTriggers> response = await fixture.GetResponseAsync<GotDeletedTriggers>(new GetDeletedTriggers());
        response.Message.Triggers.ShouldBeEquivalentTo(deletedTriggers);
    }

    [TestMethod]
    public async Task Consume_TryRestoreTrigger_ValidData_PublishesRestoreTriggerRespondsSuccess()
    {
        var deletedTrigger = TriggerGenerator.CreateTrigger(isDeleted: true);
        TriggerRequestHandlerTestFixture<TryRestoreTrigger> fixture = new TriggerRequestHandlerTestFixture<TryRestoreTrigger>()
            .WithTriggers([deletedTrigger])
            .Build();

        await fixture.GetResponseAsync<Success>(new TryRestoreTrigger(deletedTrigger.Id));
        await fixture.ShouldHavePublishedAsync<RestoreTrigger>();
    }

    [TestMethod]
    public async Task Consume_TryRestoreTrigger_UnknownTrigger_ThrowsTriggerNotFoundException()
    {
        TriggerRequestHandlerTestFixture<TryRestoreTrigger> fixture = new TriggerRequestHandlerTestFixture<TryRestoreTrigger>()
            .WithTriggers([])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryRestoreTrigger("unknown".ToGuid())));
        await fixture.ShouldNotHavePublishedAsync<RestoreTrigger>();
    }

    [TestMethod]
    public async Task Consume_TryRestoreTrigger_TriggerNotDeleted_ThrowsTriggerNotDeletedException()
    {
        var notDeletedTrigger = TriggerGenerator.CreateTrigger(isDeleted: false);
        TriggerRequestHandlerTestFixture<TryRestoreTrigger> fixture = new TriggerRequestHandlerTestFixture<TryRestoreTrigger>()
            .WithTriggers([notDeletedTrigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotDeletedException>(async () =>
            await fixture.GetResponseAsync<Success>(new TryRestoreTrigger(notDeletedTrigger.Id)));
        await fixture.ShouldNotHavePublishedAsync<RestoreTrigger>();
    }

    [TestMethod]
    public async Task Consume_DuplicateTriggerLocation_ValidData_PublishesAddTriggerLocationRespondsSuccess()
    {
        var sourceTrigger = TriggerGenerator.CreateTrigger();
        var targetTrigger = TriggerGenerator.CreateTrigger();
        var node = "a node".ToGraphNode();
        sourceTrigger.AddLocation(node, null, null);
        var sourceLocation = sourceTrigger.Locations.First();
        var duplicatedLocation = new Location(node, null, Guid.NewGuid(), targetTrigger.Id, []);

        TriggerRequestHandlerTestFixture<DuplicateTriggerLocation> fixture = new TriggerRequestHandlerTestFixture<DuplicateTriggerLocation>()
            .WithTriggers([sourceTrigger, targetTrigger])
            .WithLocationResponse(duplicatedLocation)
            .Build();

        await fixture.GetResponseAsync<Success>(new DuplicateTriggerLocation(sourceTrigger.Id, targetTrigger.Id, sourceLocation.Id));
        await fixture.ShouldHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_DuplicateTriggerLocation_UnknownSourceTrigger_ThrowsTriggerNotFoundException()
    {
        var targetTrigger = TriggerGenerator.CreateTrigger();

        TriggerRequestHandlerTestFixture<DuplicateTriggerLocation> fixture = new TriggerRequestHandlerTestFixture<DuplicateTriggerLocation>()
            .WithTriggers([targetTrigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new DuplicateTriggerLocation("unknown".ToGuid(), targetTrigger.Id, Guid.NewGuid())));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_DuplicateTriggerLocation_UnknownLocation_ThrowsException()
    {
        var sourceTrigger = TriggerGenerator.CreateTrigger();
        var targetTrigger = TriggerGenerator.CreateTrigger();
        sourceTrigger.AddLocation("a node".ToGraphNode(), null, null);

        TriggerRequestHandlerTestFixture<DuplicateTriggerLocation> fixture = new TriggerRequestHandlerTestFixture<DuplicateTriggerLocation>()
            .WithTriggers([sourceTrigger, targetTrigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidOperationException>(async () =>
            await fixture.GetResponseAsync<Success>(new DuplicateTriggerLocation(sourceTrigger.Id, targetTrigger.Id, "unknown".ToGuid())));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_DuplicateTriggerLocation_TriggerLocationAdditionThrowsException_ThrowsTriggerAssignmentFailedException()
    {
        var sourceTrigger = TriggerGenerator.CreateTrigger();
        var targetTrigger = TriggerGenerator.CreateTrigger();
        sourceTrigger.AddLocation("a node".ToGraphNode(), null, null);
        var sourceLocation = sourceTrigger.Locations.First();

        TriggerRequestHandlerTestFixture<DuplicateTriggerLocation> fixture = new TriggerRequestHandlerTestFixture<DuplicateTriggerLocation>()
            .WithTriggers([sourceTrigger, targetTrigger])
            .WithLocationResponse(null, "an exception")
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () =>
            await fixture.GetResponseAsync<Success>(new DuplicateTriggerLocation(sourceTrigger.Id, targetTrigger.Id, sourceLocation.Id)));
    }

    [TestMethod]
    public async Task Consume_DuplicateTriggerLocations_ValidData_PublishesAddTriggerLocationRespondsSuccess()
    {
        var sourceTrigger = TriggerGenerator.CreateTrigger();
        var targetTrigger = TriggerGenerator.CreateTrigger();
        sourceTrigger.AddLocation("node1".ToGraphNode(), null, null);
        sourceTrigger.AddLocation("node2".ToGraphNode(), null, null);
        var duplicatedLocation = new Location("node1".ToGraphNode(), null, Guid.NewGuid(), targetTrigger.Id, []);

        TriggerRequestHandlerTestFixture<DuplicateTriggerLocations> fixture = new TriggerRequestHandlerTestFixture<DuplicateTriggerLocations>()
            .WithTriggers([sourceTrigger, targetTrigger])
            .WithLocationResponse(duplicatedLocation)
            .Build();

        await fixture.GetResponseAsync<Success>(new DuplicateTriggerLocations(sourceTrigger.Id, targetTrigger.Id));
        await fixture.ShouldHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_DuplicateTriggerLocations_UnknownSourceTrigger_ThrowsTriggerNotFoundException()
    {
        var targetTrigger = TriggerGenerator.CreateTrigger();

        TriggerRequestHandlerTestFixture<DuplicateTriggerLocations> fixture = new TriggerRequestHandlerTestFixture<DuplicateTriggerLocations>()
            .WithTriggers([targetTrigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new DuplicateTriggerLocations("unknown".ToGuid(), targetTrigger.Id)));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_DuplicateTriggerLocations_NoLocations_RespondsSuccess()
    {
        var sourceTrigger = TriggerGenerator.CreateTrigger();
        var targetTrigger = TriggerGenerator.CreateTrigger();

        TriggerRequestHandlerTestFixture<DuplicateTriggerLocations> fixture = new TriggerRequestHandlerTestFixture<DuplicateTriggerLocations>()
            .WithTriggers([sourceTrigger, targetTrigger])
            .Build();

        await fixture.GetResponseAsync<Success>(new DuplicateTriggerLocations(sourceTrigger.Id, targetTrigger.Id));
        await fixture.ShouldNotHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_DuplicateTriggerLocations_TriggerLocationAdditionThrowsException_ThrowsTriggerAssignmentFailedException()
    {
        var sourceTrigger = TriggerGenerator.CreateTrigger();
        var targetTrigger = TriggerGenerator.CreateTrigger();
        sourceTrigger.AddLocation("a node".ToGraphNode(), null, null);

        TriggerRequestHandlerTestFixture<DuplicateTriggerLocations> fixture = new TriggerRequestHandlerTestFixture<DuplicateTriggerLocations>()
            .WithTriggers([sourceTrigger, targetTrigger])
            .WithLocationResponse(null, "an exception")
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () =>
            await fixture.GetResponseAsync<Success>(new DuplicateTriggerLocations(sourceTrigger.Id, targetTrigger.Id)));
    }
}