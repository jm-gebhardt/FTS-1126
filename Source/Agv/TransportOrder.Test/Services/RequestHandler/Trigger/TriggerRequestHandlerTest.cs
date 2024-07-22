using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Trigger;

[TestClass]
public class TriggerRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetTriggers_ValidData_RespondsGotTriggers()
    {
        List<TransportOrder.Domain.Models.Trigger.Trigger> triggers = [new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [])];
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

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async() => await fixture.GetResponseAsync<GotTrigger>(new GetTrigger("unknown".ToGuid())));
    }

    [TestMethod]
    public async Task Consume_TryUpdateConfigurationParameter_ValidData_PublishesUpdateConfigurationParameterRespondsSuccess()
    {
        var parameter = new ConfigurationParameter("parameter name", "value", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        TriggerRequestHandlerTestFixture<TryUpdateConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await fixture.GetResponseAsync<Success>(new TryUpdateConfigurationParameter(trigger.Id, parameter.Name, "new value"));
        await fixture.ShouldHavePublishedAsync<UpdateConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateConfigurationParameter_UnknownTrigger_ThrowsTriggerNotFoundException_DoesNotPublishUpdateConfigurationParameter()
    {
        var parameter = new ConfigurationParameter("parameter name", "value", string.Empty);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        TriggerRequestHandlerTestFixture<TryUpdateConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () => await fixture.GetResponseAsync<Success>(new TryUpdateConfigurationParameter("unknown".ToGuid(), parameter.Name, "new value")));
        await fixture.ShouldNotHavePublishedAsync<UpdateConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateConfigurationParameter_ValidationFails_ThrowsTriggerConfigurationUpdateFailedException_DoesNotPublishUpdateConfigurationParameter()
    {
        const string isNumber = @"^-?\d+$";
        var parameter = new ConfigurationParameter("parameter name", "0", isNumber);
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, [parameter]);
        TriggerRequestHandlerTestFixture<TryUpdateConfigurationParameter> fixture = new TriggerRequestHandlerTestFixture<TryUpdateConfigurationParameter>()
            .WithTriggers([trigger])
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerConfigurationUpdateFailedException>(async () => await fixture.GetResponseAsync<Success>(new TryUpdateConfigurationParameter(trigger.Id, parameter.Name, "new value is not number")));
        await fixture.ShouldNotHavePublishedAsync<UpdateConfigurationParameter>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_UnknownTrigger_ThrowsTriggerNotFoundException_DoesNotRequestAddTriggerLocation()
    {
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
        .WithTriggers([])
        .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerNotFoundException>(async () => await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation("unknown".ToGuid(), Guid.NewGuid(), null, [], Guid.NewGuid())));
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

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () => await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, Guid.NewGuid(), "a node", [], Guid.NewGuid())));
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

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () => await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, "unknown".ToGuid(), null, [], Guid.NewGuid())));
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

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () => await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, null, "unknown node", [], Guid.NewGuid())));
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

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async () => await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, Guid.NewGuid(), null, [], "unknown".ToGuid())));
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_ValidData_PublishesAddTriggerLocationRespondsTriggerLocationAssigned()
    {
        const string node = "a node";
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var location = new Location(node, Guid.NewGuid(), trigger.Id);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(true)
            .WithLocationResponse(location)
            .Build();

        Response<TriggerLocationAssigned> response = await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, null, location.NodeId!, [], location.MapId));
        response.Message.Location.ShouldBeEquivalentTo(location);
        await fixture.ShouldHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_MapNotGiven_PublishesAddTriggerLocationRespondsTriggerLocationAssigned()
    {
        const string node = "a node";
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var location = new Location(node, Guid.NewGuid(), trigger.Id);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(false)
            .WithLocationResponse(location)
            .Build();

        Response<TriggerLocationAssigned> response = await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, null, location.NodeId!, [], null));
        response.Message.Location.ShouldBeEquivalentTo(location);
        await fixture.ShouldHavePublishedAsync<AddTriggerLocation>();
    }

    [TestMethod]
    public async Task Consume_AssignTriggerToLocation_TriggerLocationAdditionThrowsException_ThrowsTriggerAssignmentFailedException()
    {
        const string node = "a node";
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var location = new Location(node, Guid.NewGuid(), trigger.Id);
        TriggerRequestHandlerTestFixture<AssignTriggerToLocation> fixture = new TriggerRequestHandlerTestFixture<AssignTriggerToLocation>()
            .WithTriggers([trigger])
            .WithGraphElementExists(true)
            .WithMapExists(true)
            .WithLocationResponse(null, "an exception")
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<TriggerAssignmentFailedException>(async() => await fixture.GetResponseAsync<TriggerLocationAssigned>(new AssignTriggerToLocation(trigger.Id, null, location.NodeId!, [], location.MapId)));
    }
}