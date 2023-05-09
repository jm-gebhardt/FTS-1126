using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Maps
{
    [TestClass]
    public class EfMapRegistryTest
    {
        [TestMethod]
        public void GetAllMaps_ReturnsEmpty_List_WhenRegistryIsEmpty()
        {
            var fixture = new EfMapRegistryTestFixture();

            IQueryable<IMap> maps = fixture.Registry.GetAllMaps();
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

            IQueryable<IMap> maps = fixture.Registry.GetAllMaps();
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

            byte[]? mapValue1 = fixture.Registry.GetMap(map1.CorrelationId);
            byte[]? mapValue2 = fixture.Registry.GetMap(map1.CorrelationId);

            map1.MapValue.ShouldBe(mapValue1);
            map2.MapValue.ShouldBe(mapValue2);
        }

        [TestMethod]
        public void GetMap_ShouldThrowInvalidOperationException_WhenMapIsNotFound()
        {
            var fixture = new EfMapRegistryTestFixture();

            var map = fixture.GetNewMap("basicMap");

            fixture.Add(map);

            Should.Throw<InvalidOperationException>(() => fixture.Registry.GetMap(Guid.NewGuid()));
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
        public void AddOrUpdate_ShouldReturnTrue_WhenAddingNewMap()
        {
            var fixture = new EfMapRegistryTestFixture();

            const string mapId = "basicMap";
            var mapGuid = mapId.ToGuid();
            const string imageData = "aW1hZ2VJbWFnZUlNQUdF";
            const float resolution = 1;
            const float xOffset = 1;
            const float yOffset = 1;
            var modificationDate = DateTime.UtcNow;

            bool result = fixture.Registry.AddOrUpdate(mapGuid, mapId, imageData, resolution, xOffset, yOffset, modificationDate);
            result.ShouldBeTrue();

            byte[]? map = fixture.Registry.GetMap(mapGuid);
            map.ShouldNotBeNull();
            map.ShouldBe(Convert.FromBase64String(imageData));
        }

        [TestMethod]
        public void AddOrUpdate_ShouldReturnTrue_WhenUpdatingMap()
        {
            var fixture = new EfMapRegistryTestFixture();

            string mapId = "basicMap";
            var mapGuid = mapId.ToGuid();
            string imageData = "aW1hZ2VJbWFnZUlNQUdF";
            float resolution = 1;
            float xOffset = 1;
            float yOffset = 1;
            var modificationDate = DateTime.UtcNow;

            bool result = fixture.Registry.AddOrUpdate(mapGuid, mapId, imageData, resolution, xOffset, yOffset, modificationDate);
            result.ShouldBeTrue();
            byte[]? map = fixture.Registry.GetMap(mapGuid);
            map.ShouldNotBeNull();
            map.ShouldBe(Convert.FromBase64String(imageData));

            imageData = "TkVXaW1hZ2VOZXdJbWFnZQ==";
            resolution = 2;
            xOffset = 2;
            yOffset = 2;
            modificationDate = DateTime.UtcNow;

            result = fixture.Registry.AddOrUpdate(mapGuid, mapId, imageData, resolution, xOffset, yOffset, modificationDate);
            result.ShouldBeTrue();
            map = fixture.Registry.GetMap(mapGuid);
            map.ShouldNotBeNull();
            map.ShouldBe(Convert.FromBase64String(imageData));
        }
    }
}
