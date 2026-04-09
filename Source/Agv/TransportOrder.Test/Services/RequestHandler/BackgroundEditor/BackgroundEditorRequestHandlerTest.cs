using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.BackgroundEditor;

[TestClass]
public class BackgroundEditorRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_AddOrUpdateBackground_ValidData_RespondsBackgroundAddedOrUpdated()
    {
        var background = BackgroundGenerator.Generate();
        BackgroundEditorRequestHandlerTestFixture<AddOrUpdateBackground> fixture = new BackgroundEditorRequestHandlerTestFixture<AddOrUpdateBackground>()
            .WithBackground(background);

        Response<BackgroundAddedOrUpdated> response = await fixture.GetResponseAsync<BackgroundAddedOrUpdated>(new AddOrUpdateBackground(background));

        response.Message.Background.Id.ShouldBe(background.Id);
    }

    [TestMethod]
    public async Task Consume_GetBackground_ValidData_RespondsGotBackground()
    {
        var background = BackgroundGenerator.Generate();
        BackgroundEditorRequestHandlerTestFixture<GetBackground> fixture = new BackgroundEditorRequestHandlerTestFixture<GetBackground>()
            .WithBackground(background);

        Response<GotBackground> response = await fixture.GetResponseAsync<GotBackground>(new GetBackground(background.Id));

        response.Message.Background.Id.ShouldBe(background.Id);
    }

    [TestMethod]
    public async Task Consume_GetBackgrounds_ValidData_RespondsGotBackgrounds()
    {
        var background = BackgroundGenerator.Generate();
        BackgroundEditorRequestHandlerTestFixture<GetBackgrounds> fixture = new BackgroundEditorRequestHandlerTestFixture<GetBackgrounds>()
            .WithBackground(background);

        Response<GotBackgrounds> response = await fixture.GetResponseAsync<GotBackgrounds>(new GetBackgrounds([]));

        response.Message.Backgrounds.First().Id.ShouldBe(background.Id);
    }

    [TestMethod]
    public async Task Consume_DeleteBackgroundScene_ValidData_RespondsSuccess()
    {
        var background = BackgroundGenerator.Generate();
        BackgroundEditorRequestHandlerTestFixture<DeleteBackgroundScene> fixture = new BackgroundEditorRequestHandlerTestFixture<DeleteBackgroundScene>()
            .WithBackground(background);

        await fixture.GetResponseAsync<Success>(new DeleteBackgroundScene(background.Id));
    }
}