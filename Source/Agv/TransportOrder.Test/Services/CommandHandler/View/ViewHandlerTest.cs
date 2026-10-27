using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Events.Views;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Views;
using Shouldly;
using Fixture = Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.View.ViewHandlerTestFixture;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.View;

[TestClass]
public class ViewHandlerTest
{
    [TestMethod]
    public async Task Consume_GetViews_NoStoredView_ReturnsGeneratedViewPerLevel()
    {
        var fixture = new Fixture().WithLevel();

        var response = await fixture.GetResponseAsync<GotViews>(new GetViews(Guid.NewGuid()));

        var view = response.Message.Views.ShouldHaveSingleItem();
        view.IsGenerated.ShouldBeTrue();
        view.LevelId.ShouldBe(Fixture.LevelId);
    }

    [TestMethod]
    public async Task Consume_GetViews_StoredView_ReturnsOnlyStoredViews()
    {
        var fixture = new Fixture().WithLevel().WithStoredView(out var storedView);

        var response = await fixture.GetResponseAsync<GotViews>(new GetViews(Guid.NewGuid()));

        response.Message.Views.ShouldHaveSingleItem().Id.ShouldBe(storedView.Id);
    }

    [TestMethod]
    public async Task Consume_GetView_UnknownId_ThrowsViewNotFoundException()
    {
        var fixture = new Fixture().WithLevel();

        await MediatorShould.ThrowRequestExceptionAsync<ViewNotFoundException>(() => fixture.GetResponseAsync<GetView, GotView>(new GetView(Fixture.UnknownViewId)));
    }

    [TestMethod]
    public async Task Consume_GetView_GeneratedIdWhileViewIsStored_ThrowsViewNotFoundException()
    {
        var fixture = new Fixture().WithLevel().WithStoredView(out _);
        var generatedView = fixture.GetGeneratedView();

        await MediatorShould.ThrowRequestExceptionAsync<ViewNotFoundException>(() => fixture.GetResponseAsync<GetView, GotView>(new GetView(generatedView.Id)));
    }

    [TestMethod]
    public async Task Consume_AddView_ValidData_StoresViewAndPublishesViewChanged()
    {
        var fixture = new Fixture();
        var viewId = Guid.NewGuid();

        var response = await fixture.GetResponseAsync<AddView, GotView>(new AddView(viewId, Fixture.NewViewName, Fixture.LevelId, IsDefault: true, Fixture.Area));

        response.Message.View.Id.ShouldBe(viewId);
        fixture.StoredViews.ShouldContainKey(viewId);
        (await fixture.GetFirstPublishedMessageAsync<ViewChanged>()).View.Id.ShouldBe(viewId);
    }

    [TestMethod]
    public async Task Consume_UpdateView_GeneratedView_StoresItAsUserDefinedView()
    {
        var fixture = new Fixture().WithLevel();
        var generatedView = fixture.GetGeneratedView();

        var response = await fixture.GetResponseAsync<UpdateView, GotView>(new UpdateView(generatedView.Id, Fixture.NewViewName, null, null, null));

        response.Message.View.IsGenerated.ShouldBeFalse();
        fixture.StoredViews[generatedView.Id].Name.ShouldBe(Fixture.NewViewName);
        await fixture.ShouldHavePublishedAsync<ViewChanged>();
    }

    [TestMethod]
    public async Task Consume_DeleteView_GeneratedView_DeletesNothing()
    {
        var fixture = new Fixture().WithLevel();
        var generatedView = fixture.GetGeneratedView();

        await fixture.GetResponseAsync<DeleteView, Success>(new DeleteView(generatedView.Id));

        await fixture.ShouldNotHavePublishedAsync<ViewDeleted>();
    }

    [TestMethod]
    public async Task Consume_DeleteView_StoredView_DeletesViewAndPublishesViewDeleted()
    {
        var fixture = new Fixture().WithStoredView(out var storedView);

        await fixture.GetResponseAsync<DeleteView, Success>(new DeleteView(storedView.Id));

        fixture.StoredViews.ShouldNotContainKey(storedView.Id);
        (await fixture.GetFirstPublishedMessageAsync<ViewDeleted>()).CorrelationId.ShouldBe(storedView.Id);
    }

    [TestMethod]
    public async Task Consume_SetViewBackground_Png_StoresBackgroundAndImage()
    {
        var fixture = new Fixture().WithStoredView(out var storedView);

        await fixture.GetResponseAsync<SetViewBackground, Success>(new SetViewBackground(storedView.Id, Fixture.Resolution, Fixture.XOffset, Fixture.YOffset, Fixture.PngImage));

        fixture.StoredViews[storedView.Id].Background.ShouldNotBeNull().Resolution.ShouldBe(Fixture.Resolution);
        fixture.StoredImages[storedView.Id].ShouldBe(Fixture.PngImage);
    }

    [TestMethod]
    public async Task Consume_SetViewBackground_Jpeg_ThrowsBitmapDecodingException()
    {
        var fixture = new Fixture().WithStoredView(out var storedView);

        await MediatorShould.ThrowRequestExceptionAsync<BitmapDecodingException>(() =>
            fixture.GetResponseAsync<SetViewBackground, Success>(new SetViewBackground(storedView.Id, Fixture.Resolution, Fixture.XOffset, Fixture.YOffset, Fixture.JpegImage)));

        fixture.StoredImages.ShouldBeEmpty();
    }
}