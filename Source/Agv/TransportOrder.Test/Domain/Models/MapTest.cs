using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Domain.Models;

[TestClass]
public class MapTest
{
    private const string Base64String = "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAIAQMAAAD+wSzIAAAABlBMVEX///+/v7+jQ3Y5AAAADklEQVQI12P4AIX8EAgALgAD/aNpbtEAAAAASUVORK5CYII=";

    [TestMethod]
    public void Create_ValidData_AllPropertiesSet()
    {
        const string mapId = "mapId";
        var correlationId = mapId.ToGuid();
        const float resolution = 0.3f;
        const float xOffSet = 1;
        const float yOffSet = 1;
        var now = DateTime.UtcNow;
        var map = new Map(
            correlationId,
            mapId,
            Convert.FromBase64String(Base64String),
            8,
            8,
            resolution,
            xOffSet,
            yOffSet,
            now
        );
        map.CorrelationId.ShouldBe(correlationId);
        map.MapId.ShouldBe(mapId);
        map.MapValue.ShouldBe(Convert.FromBase64String(Base64String));
        map.ModificationDate.ShouldBe(now);
        map.Height.ShouldBe(8);
        map.Width.ShouldBe(8);
        map.Resolution.ShouldBe(resolution);
        map.XOffset.ShouldBe(xOffSet);
        map.YOffset.ShouldBe(yOffSet);
    }

    [TestMethod]
    public void Create_NoImageProvided_AllPropertiesSet()
    {
        const string mapId = "mapId";
        var correlationId = mapId.ToGuid();
        const float resolution = 0.3f;
        const float xOffSet = 1;
        const float yOffSet = 1;
        var now = DateTime.UtcNow;
        var map = new Map(
            correlationId,
            mapId,
            Array.Empty<byte>(),
            0,
            0,
            resolution,
            xOffSet,
            yOffSet,
            now
        );
        map.CorrelationId.ShouldBe(correlationId);
        map.MapId.ShouldBe(mapId);
        map.MapValue.ShouldBe(Array.Empty<byte>());
        map.ModificationDate.ShouldBe(now);
        map.Height.ShouldBe(0);
        map.Width.ShouldBe(0);
        map.Resolution.ShouldBe(resolution);
        map.XOffset.ShouldBe(xOffSet);
        map.YOffset.ShouldBe(yOffSet);
    }

    [TestMethod]
    public void Create_InvalidImageProvided_AllPropertiesSet()
    {
        const string mapId = "mapId";
        var correlationId = mapId.ToGuid();
        const float resolution = 0.3f;
        const float xOffSet = 1;
        const float yOffSet = 1;
        var now = DateTime.UtcNow;
        var map = new Map(
            correlationId,
            mapId,
            null,
            0,
            0,
            resolution,
            xOffSet,
            yOffSet,
            now
        );
        map.CorrelationId.ShouldBe(correlationId);
        map.MapId.ShouldBe(mapId);
        map.MapValue.ShouldBeNull();
        map.ModificationDate.ShouldBe(now);
        map.Height.ShouldBe(0);
        map.Width.ShouldBe(0);
        map.Resolution.ShouldBe(resolution);
        map.XOffset.ShouldBe(xOffSet);
        map.YOffset.ShouldBe(yOffSet);
    }
}