using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
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

        var map1 = fixture.GetNewMap();
        var map2 = fixture.GetNewMap("2");

        fixture.Add(map1);
        fixture.Add(map2);

        ICollection<Map> maps = await fixture.Repository.GetAllMapsAsync();
        maps.ShouldNotBeEmpty();
        maps.Count.ShouldBe(2);
    }

    [TestMethod]
    public void GetMapImage_ShouldReturnMapImage_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map1 = fixture.GetNewMap();

        fixture.Add(map1);

        byte[]? mapValue1 = fixture.Repository.GetMapImage(map1.CorrelationId);

        map1.MapValue.ShouldBe(mapValue1);
    }

    [TestMethod]
    public async Task GetMapAsync_ShouldContainMapAgv_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = fixture.GetNewMap();
        map.MapAgvs.Add(new MapAgv(map.CorrelationId, map, Guid.NewGuid(), null));
        fixture.Add(map);

        var actualMap = await fixture.Repository.GetMapAsync(map.CorrelationId);

        actualMap.MapAgvs.Count.ShouldBe(1);
        actualMap.MapAgvs.Single().MapId.ShouldBe(map.CorrelationId);
        actualMap.MapAgvs.Single().AgvId.ShouldBe(actualMap.MapAgvs.Single().AgvId);
    }

    [TestMethod]
    public void GetMap_ShouldThrowInvalidOperationException_WhenMapIsNotFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = fixture.GetNewMap();

        fixture.Add(map);

        fixture.Repository.GetMapImage(Guid.NewGuid()).ShouldBeNull();
    }

    [TestMethod]
    public void Exists_ShouldReturnTrue_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = fixture.GetNewMap();

        fixture.Add(map);

        bool found = fixture.Repository.Exists(map.CorrelationId);
        found.ShouldBeTrue();
    }

    [TestMethod]
    public void Exists_ShouldReturnFalse_WhenMapIsNotFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = fixture.GetNewMap();

        fixture.Add(map);

        bool found = fixture.Repository.Exists(Guid.NewGuid());
        found.ShouldBeFalse();
    }

    [TestMethod]
    public async Task AddOrUpdate_ShouldReturnTrue_WhenAddingNewMap()
    {
        var fixture = new EfMapRepositoryTestFixture();
        var map = fixture.GetNewMap();

        bool result = await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);
        result.ShouldBeTrue();

        byte[]? mapImage = fixture.Repository.GetMapImage(map.CorrelationId);
        mapImage.ShouldNotBeNull();
        mapImage.ShouldBe(map.MapValue);
    }

    [TestMethod]
    public async Task AddOrUpdate_ShouldReturnTrue_WhenUpdatingMap()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = fixture.GetNewMap();

        bool result = await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);
        result.ShouldBeTrue();

        byte[]? mapImage = fixture.Repository.GetMapImage(map.CorrelationId);
        mapImage.ShouldNotBeNull();
        mapImage.ShouldBe(map.MapValue);

        var mapUpdate = new Map(
            "mapId".ToGuid(),
            "mapId",
            Convert.FromBase64String(EfMapRepositoryTestFixture.SmallImage2),
            1,
            1,
            2,
            2,
            2,
            DateTime.UtcNow
        );

        result = await fixture.Repository.AddOrUpdateAsync(mapUpdate, CancellationToken.None);
        result.ShouldBeTrue();
        mapImage = fixture.Repository.GetMapImage(mapUpdate.CorrelationId);
        mapImage.ShouldNotBeNull();
        mapImage.ShouldBe(mapUpdate.MapValue);
    }
}