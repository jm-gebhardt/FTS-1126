using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.RestrictedArea;

[TestClass]
public class RestrictedAreaRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_AddRestrictedArea_ValidData_SaveRestrictedAreaPublished()
    {
        var fixture = new RestrictedAreaRequestHandlerTestFixture<AddRestrictedArea>().Build();

        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var response = await fixture.GetResponseAsync<RestrictedAreaAdded>(new AddRestrictedArea(polygon));

        response.Message.RestrictedArea.Area.Count.ShouldBe(polygon.Count);

        (await fixture.GetFirstPublishedMessageAsync<SaveRestrictedArea>()).RestrictedArea.ShouldBeEquivalentTo(response.Message.RestrictedArea);
    }

    [TestMethod]
    public async Task Consume_AddRestrictedArea_PolygonWithLessThanThreePoints_ThrowsInvalidPolygonLengthException()
    {
        var fixture = new RestrictedAreaRequestHandlerTestFixture<AddRestrictedArea>().Build();

        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1)};

        await MediatorShould.ThrowRequestExceptionAsync<InvalidPolygonLengthException>(async () => await fixture.GetResponseAsync<RestrictedAreaAdded>(new AddRestrictedArea(polygon)));
    }

    [TestMethod]
    public async Task Consume_TryUpdateRestrictedArea_ValidData_UpdateRestrictedArea()
    {
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(2, 2, 2) });
        var fixture = new RestrictedAreaRequestHandlerTestFixture<TryUpdateRestrictedArea>()
            .WithRestrictedArea(restrictedArea)
            .Build();

        var response = await fixture.GetResponseAsync<Success>(new TryUpdateRestrictedArea(restrictedArea.Id, new List<PolygonPoint> { new(2, 1, 0), new(1, 3, 1), new(2, 2, 2) }));

        response.ShouldNotBeNull();
        await fixture.ShouldHavePublishedAsync<UpdateRestrictedArea>();
    }

    [TestMethod]
    public async Task Consume_TryUpdateRestrictedArea_PolygonWithLessThanThreePoints_ThrowsInvalidPolygonLengthException()
    {
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(2, 2, 2) });
        var fixture = new RestrictedAreaRequestHandlerTestFixture<TryUpdateRestrictedArea>()
            .WithRestrictedArea(restrictedArea)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidPolygonLengthException>(async () => await fixture.GetResponseAsync<RestrictedAreaAdded>(new TryUpdateRestrictedArea(restrictedArea.Id, new List<PolygonPoint> { new(2, 1, 0), new(1, 3, 1)})));
    }

    [TestMethod]
    public async Task Consume_TryDeleteRestrictedArea_ValidData_DeleteRestrictedArea()
    {
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(2, 2, 2) });
        var fixture = new RestrictedAreaRequestHandlerTestFixture<TryDeleteRestrictedArea>()
            .WithRestrictedArea(restrictedArea)
            .Build();

        var response = await fixture.GetResponseAsync<Success>(new TryDeleteRestrictedArea(restrictedArea.Id));
        response.ShouldNotBeNull();

        (await fixture.GetFirstPublishedMessageAsync<DeleteRestrictedArea>()).RestrictedAreaId.ShouldBe(restrictedArea.Id);
    }

    [TestMethod]
    public async Task Consume_GetRestrictedArea_ValidData_GotRestrictedArea()
    {
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(2, 2, 2) });
        var fixture = new RestrictedAreaRequestHandlerTestFixture<GetRestrictedArea>()
            .WithRestrictedArea(restrictedArea)
            .Build();

        (await fixture.GetResponseAsync<GotRestrictedArea>(new GetRestrictedArea(restrictedArea.Id)))
            .Message.RestrictedArea.ShouldBeEquivalentTo(restrictedArea);
    }

    [TestMethod]
    public async Task Consume_GetRestrictedAreas_ValidData_GotRestrictedAreas()
    {
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(2, 2, 2) });
        var fixture = new RestrictedAreaRequestHandlerTestFixture<GetRestrictedAreas>()
            .WithRestrictedArea(restrictedArea)
            .Build();

        (await fixture.GetResponseAsync<GotRestrictedAreas>(new GetRestrictedAreas([restrictedArea.Id])))
            .Message.RestrictedAreas.ShouldBeEquivalentTo(new List<TransportOrder.Domain.Models.RestrictedArea.RestrictedArea> { restrictedArea });
    }
}