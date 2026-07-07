using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;
using Gebhardt.Acc.TransportOrder.Messages.Requests.AgvType;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.AgvType;

[TestClass]
public class AgvTypeRequestHandlerTest
{
    private static TransportOrder.Domain.Models.AgvTypes.AgvType CreateAgvType(string identifier = "AGV1") =>
        new(identifier, "Gebhardt", "SeriesA", length: 1.5, width: 0.8, acknowledged: false);

    [TestMethod]
    public async Task Consume_RequestAgvTypeAcknowledgement_ExistingAgvType_PublishesAcknowledgeAgvTypeAndRespondsSuccess()
    {
        var agvType = CreateAgvType();
        AgvTypeRequestHandlerTestFixture<RequestAgvTypeAcknowledgement, AgvTypeRequestHandler> fixture =
            new AgvTypeRequestHandlerTestFixture<RequestAgvTypeAcknowledgement, AgvTypeRequestHandler>()
                .WithAgvTypes(agvType)
                .Build();

        await fixture.GetResponseAsync<Success>(new RequestAgvTypeAcknowledgement(agvType.Id));

        await fixture.ShouldHavePublishedAsync<AcknowledgeAgvType>();
    }

    [TestMethod]
    public async Task Consume_RequestAgvTypeAcknowledgement_UnknownAgvType_ThrowsAgvTypeNotFoundException()
    {
        AgvTypeRequestHandlerTestFixture<RequestAgvTypeAcknowledgement, AgvTypeRequestHandler> fixture =
            new AgvTypeRequestHandlerTestFixture<RequestAgvTypeAcknowledgement, AgvTypeRequestHandler>()
                .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvTypeNotFoundException>(async () =>
            await fixture.GetResponseAsync<Success>(new RequestAgvTypeAcknowledgement(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task Consume_GetAgvType_ExistingAgvType_RespondsWithAgvType()
    {
        var agvType = CreateAgvType();
        AgvTypeRequestHandlerTestFixture<GetAgvType, AgvTypeRequestHandler> fixture =
            new AgvTypeRequestHandlerTestFixture<GetAgvType, AgvTypeRequestHandler>()
                .WithAgvTypes(agvType)
                .Build();

        Response<GotAgvType> response = await fixture.GetResponseAsync<GotAgvType>(new GetAgvType(agvType.Id));

        response.Message.CorrelationId.ShouldBe(agvType.Id);
        response.Message.AgvType.Id.ShouldBe(agvType.Id);
    }

    [TestMethod]
    public async Task Consume_GetAgvType_UnknownAgvType_ThrowsAgvTypeNotFoundException()
    {
        AgvTypeRequestHandlerTestFixture<GetAgvType, AgvTypeRequestHandler> fixture =
            new AgvTypeRequestHandlerTestFixture<GetAgvType, AgvTypeRequestHandler>()
                .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvTypeNotFoundException>(async () =>
            await fixture.GetResponseAsync<GotAgvType>(new GetAgvType(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task Consume_GetAllAgvTypes_EmptyRepository_ReturnsEmptyList()
    {
        AgvTypeRequestHandlerTestFixture<GetAllAgvTypes, AgvTypeRequestHandler> fixture =
            new AgvTypeRequestHandlerTestFixture<GetAllAgvTypes, AgvTypeRequestHandler>()
                .Build();

        Response<GotAgvTypes> response = await fixture.GetResponseAsync<GotAgvTypes>(new GetAllAgvTypes(Guid.NewGuid()));

        response.Message.AgvTypes.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_GetAllAgvTypes_MultipleAgvTypes_ReturnsAllAgvTypes()
    {
        var agvType1 = CreateAgvType("AGV1");
        var agvType2 = CreateAgvType("AGV2");
        AgvTypeRequestHandlerTestFixture<GetAllAgvTypes, AgvTypeRequestHandler> fixture =
            new AgvTypeRequestHandlerTestFixture<GetAllAgvTypes, AgvTypeRequestHandler>()
                .WithAgvTypes(agvType1, agvType2)
                .Build();

        Response<GotAgvTypes> response = await fixture.GetResponseAsync<GotAgvTypes>(new GetAllAgvTypes(Guid.NewGuid()));

        response.Message.AgvTypes.Count.ShouldBe(2);
        response.Message.AgvTypes.ShouldContain(a => a.Id == agvType1.Id);
        response.Message.AgvTypes.ShouldContain(a => a.Id == agvType2.Id);
    }
}