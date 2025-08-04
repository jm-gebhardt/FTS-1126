using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfMapRepositoryTest
{
    [TestMethod]
    public async Task GetAllMaps_ReturnsEmpty_List_WhenRepositoryIsEmpty()
    {
        var fixture = new EfMapRepositoryTestFixture();

        ICollection<Map> maps = await fixture.Repository.GetAllMapsAsync();
        maps.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAllMaps_ShouldReturnListOfMaps()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map1 = MapGenerator.Generate();
        var map2 = MapGenerator.Generate();

        fixture.Add(map1);
        fixture.Add(map2);

        ICollection<Map> maps = await fixture.Repository.GetAllMapsAsync();
        maps.ShouldNotBeEmpty();
        maps.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task GetBackgroundImageAsync_ShouldReturnMapImage_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map1 = MapGenerator.Generate();

        fixture.Add(map1);

        byte[] backgroundImage = await fixture.Repository.GetBackgroundImageAsync(map1.CorrelationId);

        backgroundImage.ShouldBe(map1.BaseBackground!.Image);
    }

    [TestMethod]
    public async Task GetMapAsync_ShouldContainMapAgv_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = MapGenerator.Generate();
        map.MapAgvs.Add(new MapAgv(map.CorrelationId, map, Guid.NewGuid(), null));
        fixture.Add(map);

        var actualMap = await fixture.Repository.GetMapAsync(map.CorrelationId);

        actualMap.MapAgvs.Count.ShouldBe(1);
        actualMap.MapAgvs.Single().MapId.ShouldBe(map.CorrelationId);
        actualMap.MapAgvs.Single().AgvId.ShouldBe(actualMap.MapAgvs.Single().AgvId);
    }

    [TestMethod]
    public void Exists_ShouldReturnTrue_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = MapGenerator.Generate();

        fixture.Add(map);

        bool found = fixture.Repository.Exists(map.CorrelationId);
        found.ShouldBeTrue();
    }

    [TestMethod]
    public void Exists_ShouldReturnFalse_WhenMapIsNotFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = MapGenerator.Generate();

        fixture.Add(map);

        bool found = fixture.Repository.Exists(Guid.NewGuid());
        found.ShouldBeFalse();
    }

    [TestMethod]
    public async Task AddOrUpdate_ShouldReturnTrue_WhenAddingNewMap()
    {
        var fixture = new EfMapRepositoryTestFixture();
        var map = MapGenerator.Generate();

        bool result = await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);
        result.ShouldBeTrue();

        var background = await fixture.Repository.GetMapAsync(map.CorrelationId);
        background.BaseBackground.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task AddOrUpdate_ShouldReturnTrue_WhenUpdatingMap()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = MapGenerator.Generate();

        bool result = await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);
        result.ShouldBeTrue();

        var background = await fixture.Repository.GetMapAsync(map.CorrelationId);
        background.BaseBackground.ShouldNotBeNull();
        background.BaseBackground.Image.ShouldBe(map.BaseBackground!.Image);

        const string mapNew = "map-new";
        const int width = 22;
        const int height = 33;
        const double resolution = 44;
        const double xOffset = 55;
        const double yOffset = 66;
        var mapUpdate = MapGenerator.Generate(
            mapNew.ToGuid(),
            Convert.FromBase64String(EfMapRepositoryTestFixture.SmallImage2),
            mapNew,
            width,
            height,
            resolution,
            xOffset,
            yOffset,
            DateTime.Now);

        result = await fixture.Repository.AddOrUpdateAsync(mapUpdate, CancellationToken.None);
        result.ShouldBeTrue();
        background = await fixture.Repository.GetMapAsync(mapUpdate.CorrelationId);
        background.BaseBackground.ShouldNotBeNull();
        background.BaseBackground.Image.ShouldNotBeNull();
        background.BaseBackground.Image.ShouldBe(Convert.FromBase64String(EfMapRepositoryTestFixture.SmallImage2));
        background.BaseBackground.Width.ShouldBe(width);
        background.BaseBackground.Height.ShouldBe(height);
        background.BaseBackground.Resolution.ShouldBe(resolution);
        background.BaseBackground.XOffset.ShouldBe(xOffset);
        background.BaseBackground.YOffset.ShouldBe(yOffset);
    }
}