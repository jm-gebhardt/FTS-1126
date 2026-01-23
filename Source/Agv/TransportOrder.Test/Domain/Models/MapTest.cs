using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

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
        const double xOffSet = 1;
        const double yOffSet = 1;
        const int width = 8;
        const int height = 8;
        var now = DateTime.UtcNow;
        var map = MapGenerator.Generate(correlationId,
            Convert.FromBase64String(Base64String), mapId,
            width,
            height,
            resolution,
            xOffSet,
            yOffSet,
            now);

        map.CorrelationId.ShouldBe(correlationId);
        map.MapName.ShouldBe(mapId);
        map.ModificationDate.ShouldBe(now);
        map.BaseBackground.ShouldNotBeNull();
        map.BaseBackground.Image.ShouldBe(Convert.FromBase64String(Base64String));

        map.BaseBackground.Height.ShouldBe(width);
        map.BaseBackground.Width.ShouldBe(height);
        map.BaseBackground.Resolution.ShouldBe(resolution);
        map.BaseBackground.XOffset.ShouldBe(xOffSet);
        map.BaseBackground.YOffset.ShouldBe(yOffSet);
    }

    [TestMethod]
    public void Create_NoImageProvided_AllPropertiesSet()
    {
        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var map = MapGenerator.Generate(
            correlationId,
            background: null,
            modificationDate: now
        );
        map.CorrelationId.ShouldBe(correlationId);
        map.ModificationDate.ShouldBe(now);
        map.BaseBackground.ShouldBeNull();
    }
}