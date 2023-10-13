using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class EfMapRegistryTest
{
    [TestMethod]
    public void GetAllMaps_ReturnsEmpty_List_WhenRegistryIsEmpty()
    {
        var fixture = new EfMapRegistryTestFixture();

        IQueryable<Map> maps = fixture.Registry.GetAllMaps();
        maps.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetAllMaps_ShouldReturnListOfMaps()
    {
        var fixture = new EfMapRegistryTestFixture();

        var map1 = fixture.GetNewMap("basicMap1");
        var map2 = fixture.GetNewMap("basicMap2");

        fixture.Add(map1);
        fixture.Add(map2);

        IQueryable<Map> maps = fixture.Registry.GetAllMaps();
        maps.ShouldNotBeEmpty();
        maps.Count().ShouldBe(2);
    }

    [TestMethod]
    public void GetMap_ShouldReturnMapValue_WhenMapIsFound()
    {
        var fixture = new EfMapRegistryTestFixture();

        var map1 = fixture.GetNewMap("basicMap1");
        var map2 = fixture.GetNewMap("basicMap2");

        fixture.Add(map1);
        fixture.Add(map2);

        byte[]? mapValue1 = fixture.Registry.GetMapImage(map1.CorrelationId);
        byte[]? mapValue2 = fixture.Registry.GetMapImage(map1.CorrelationId);

        map1.MapValue.ShouldBe(mapValue1);
        map2.MapValue.ShouldBe(mapValue2);
    }

    [TestMethod]
    public void GetMap_ShouldThrowInvalidOperationException_WhenMapIsNotFound()
    {
        var fixture = new EfMapRegistryTestFixture();

        var map = fixture.GetNewMap("basicMap");

        fixture.Add(map);

        fixture.Registry.GetMapImage(Guid.NewGuid()).ShouldBeNull();
    }

    [TestMethod]
    public void Exists_ShouldReturnTrue_WhenMapIsFound()
    {
        var fixture = new EfMapRegistryTestFixture();

        var map = fixture.GetNewMap("basicMap");

        fixture.Add(map);

        bool found = fixture.Registry.Exists(map.CorrelationId);
        found.ShouldBeTrue();
    }

    [TestMethod]
    public void Exists_ShouldReturnFalse_WhenMapIsNotFound()
    {
        var fixture = new EfMapRegistryTestFixture();

        var map = fixture.GetNewMap("basicMap");

        fixture.Add(map);

        bool found = fixture.Registry.Exists(Guid.NewGuid());
        found.ShouldBeFalse();
    }

    [TestMethod]
    public async Task AddOrUpdate_ShouldReturnTrue_WhenAddingNewMap()
    {
        var fixture = new EfMapRegistryTestFixture();
        var map = fixture.GetNewMap("mapId");

        bool result = await fixture.Registry.AddOrUpdateAsync(map, new CancellationToken());
        result.ShouldBeTrue();

        byte[]? mapImage = fixture.Registry.GetMapImage(map.CorrelationId);
        mapImage.ShouldNotBeNull();
        mapImage.ShouldBe(map.MapValue);
    }

    [TestMethod]
    public async Task AddOrUpdate_ShouldReturnTrue_WhenUpdatingMap()
    {
        var fixture = new EfMapRegistryTestFixture();

        var map = fixture.GetNewMap("mapId");

        bool result = await fixture.Registry.AddOrUpdateAsync(map, new CancellationToken());
        result.ShouldBeTrue();

        byte[]? mapImage = fixture.Registry.GetMapImage(map.CorrelationId);
        mapImage.ShouldNotBeNull();
        mapImage.ShouldBe(map.MapValue);

        var mapUpdate = new Map(
            "mapId".ToGuid(),
            "mapId",
            Convert.FromBase64String(EfMapRegistryTestFixture.SmallImage2),
            1,
            1,
            2,
            2,
            2,
            DateTime.UtcNow
        );

        result = await fixture.Registry.AddOrUpdateAsync(mapUpdate, new CancellationToken());
        result.ShouldBeTrue();
        mapImage = fixture.Registry.GetMapImage(mapUpdate.CorrelationId);
        mapImage.ShouldNotBeNull();
        mapImage.ShouldBe(mapUpdate.MapValue);
    }
}