using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfBackgroundEditorRepositoryTest
{
    [TestMethod]
    public async Task AddOrUpdateBackgroundAsync_NewBackground_BackgroundAdded()
    {
        var background = BackgroundGenerator.Generate();
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();

        await fixture.Repository.AddOrUpdateBackgroundAsync(background);

        (await fixture.Repository.GetRequiredBackgroundAsync(background.Id)).Id.ShouldBeEquivalentTo(background.Id);
    }

    [TestMethod]
    public async Task AddOrUpdateBackgroundAsync_ExistingBackgroundWithSameNameAndMapId_BackgroundReplaced()
    {
        var background = BackgroundGenerator.Generate();
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();
        await fixture.Repository.AddOrUpdateBackgroundAsync(background);

        var updatedBackground = BackgroundGenerator.Generate(id: Guid.NewGuid());

        await fixture.Repository.AddOrUpdateBackgroundAsync(updatedBackground);

        ICollection<Background> backgrounds = await fixture.Repository.GetBackgroundsAsync([]);
        backgrounds.Count.ShouldBe(1);
        backgrounds.Single().Id.ShouldBe(updatedBackground.Id);
    }

    [TestMethod]
    public async Task AddOrUpdateBackgroundAsync_DifferentName_BothBackgroundsExist()
    {
        var background1 = BackgroundGenerator.Generate();
        var background2 = BackgroundGenerator.Generate(id: Guid.NewGuid(), name: "OtherBackground");
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();

        await fixture.Repository.AddOrUpdateBackgroundAsync(background1);
        await fixture.Repository.AddOrUpdateBackgroundAsync(background2);

        (await fixture.Repository.GetBackgroundsAsync([])).Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task GetRequiredBackgroundAsync_ExistingBackground_ReturnsBackground()
    {
        var background = BackgroundGenerator.Generate();
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();
        await fixture.AddAsync(background);

        (await fixture.Repository.GetRequiredBackgroundAsync(background.Id)).Id.ShouldBeEquivalentTo(background.Id);
    }

    [TestMethod]
    public async Task GetRequiredBackgroundAsync_UnknownId_ThrowsBackgroundNotFoundException()
    {
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();

        await Should.ThrowAsync<BackgroundNotFoundException>(() =>
            fixture.Repository.GetRequiredBackgroundAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task DeleteBackgroundAsync_ExistingBackground_BackgroundRemoved()
    {
        var background = BackgroundGenerator.Generate();
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();
        await fixture.AddAsync(background);

        await fixture.Repository.DeleteBackgroundAsync(background.Id);

        (await fixture.Repository.GetBackgroundsAsync([])).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task DeleteBackgroundAsync_UnknownId_ThrowsBackgroundNotFoundException()
    {
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();

        await Should.ThrowAsync<BackgroundNotFoundException>(() =>
            fixture.Repository.DeleteBackgroundAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task GetBackgroundsAsync_WithMatchingMapIds_ReturnsMatchingBackgrounds()
    {
        var background = BackgroundGenerator.Generate();
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();
        await fixture.AddAsync(background);

        ICollection<Background> result = await fixture.Repository.GetBackgroundsAsync([BackgroundGenerator.DefaultMapId]);

        result.First().Id.ShouldBe(background.Id);
    }

    [TestMethod]
    public async Task GetBackgroundsAsync_WithNonMatchingMapId_ReturnsEmpty()
    {
        var background = BackgroundGenerator.Generate();
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();
        await fixture.AddAsync(background);

        ICollection<Background> result = await fixture.Repository.GetBackgroundsAsync([Guid.NewGuid()]);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetBackgroundsAsync_NoMapIdsGiven_ReturnsAllBackgrounds()
    {
        var background = BackgroundGenerator.Generate();
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();
        await fixture.AddAsync(background);

        ICollection<Background> result = await fixture.Repository.GetBackgroundsAsync([]);

        result.First().Id.ShouldBe(background.Id);
    }

    [TestMethod]
    public async Task GetBackgroundsAsync_MaxCountRespected_ReturnsLimitedResults()
    {
        var background1 = BackgroundGenerator.Generate();
        var background2 = BackgroundGenerator.Generate(id: Guid.NewGuid(), name: "OtherBackground");
        await using var fixture = new EfBackgroundEditorRepositoryTestFixture();
        await fixture.AddAsync(background1);
        await fixture.AddAsync(background2);

        ICollection<Background> result = await fixture.Repository.GetBackgroundsAsync([], maxCount: 1);

        result.Count.ShouldBe(1);
    }
}
