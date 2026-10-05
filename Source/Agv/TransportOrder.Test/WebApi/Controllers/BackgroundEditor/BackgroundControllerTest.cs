using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.BackgroundEditor;

[TestClass]
public class BackgroundControllerTest
{
    [TestMethod]
    public async Task AddOrUpdateBackground_ValidData_ReturnsOk()
    {
        var background = BackgroundGenerator.Generate();
        var fixture = new BackgroundControllerTestFixture()
            .WithBackground(background)
            .Build();

        var backgroundDto = fixture.Mapper.Map<BackgroundDto>(background);

        await fixture.GetControllerResultAsync<ActionResult>(c => c.AddOrUpdateBackground(backgroundDto));
    }

    [TestMethod]
    public async Task GetBackground_ValidData_ReturnsBackgroundDto()
    {
        var background = BackgroundGenerator.Generate();
        var fixture = new BackgroundControllerTestFixture()
            .WithBackground(background)
            .Build();

        var result = await fixture.GetControllerResultValueAsync(c => c.GetBackground(background.Id));

        result.Id.ShouldBe(background.Id);
        result.Name.ShouldBe(background.Name);
        result.MapId.ShouldBe(background.MapId);
    }

    [TestMethod]
    public async Task DeleteBackground_ValidData_ReturnsOk()
    {
        var fixture = new BackgroundControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.DeleteBackground(BackgroundGenerator.DefaultId));
    }

    [TestMethod]
    public async Task GetBackgrounds_WithMapIds_ReturnsMatchingBackgroundDtos()
    {
        var background = BackgroundGenerator.Generate();
        var fixture = new BackgroundControllerTestFixture()
            .WithBackground(background)
            .Build();

        var result = await fixture.GetControllerResultValueAsync(
            c => c.GetBackgrounds([BackgroundGenerator.DefaultMapId], maxCount: 50));

        result.Count.ShouldBe(1);
        result.First().Id.ShouldBe(background.Id);
    }

    [TestMethod]
    public async Task GetBackgrounds_NoMapIds_ReturnsAllBackgroundDtos()
    {
        var background1 = BackgroundGenerator.Generate();
        var background2 = BackgroundGenerator.Generate(id: Guid.NewGuid(), name: "OtherBackground");
        var fixture = new BackgroundControllerTestFixture()
            .WithBackgrounds([background1, background2])
            .Build();

        var result = await fixture.GetControllerResultValueAsync(
            c => c.GetBackgrounds(null, maxCount: 50));

        result.Count.ShouldBe(2);
    }
}
