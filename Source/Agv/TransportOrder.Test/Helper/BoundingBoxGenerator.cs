using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class BoundingBoxGenerator
{
    private static IReadOnlyList<PolygonPoint> Square(double centerX, double centerY, double halfSize)
    {
        return
        [
            new PolygonPoint(centerX - halfSize, centerY - halfSize),
            new PolygonPoint(centerX + halfSize, centerY - halfSize),
            new PolygonPoint(centerX + halfSize, centerY + halfSize),
            new PolygonPoint(centerX - halfSize, centerY + halfSize)
        ];
    }

    public static ResourceBoundingBox SquareBox(Guid resourceId, ResourceType type, double centerX, double centerY, double halfSize, string agvType = "T1")
    {
        return PolygonBox(resourceId, type, Square(centerX, centerY, halfSize), agvType);
    }

    public static ResourceBoundingBox PolygonBox(Guid resourceId, ResourceType type, IReadOnlyList<PolygonPoint> polygon, string agvType = "T1")
    {
        return new ResourceBoundingBox
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            ResourceType = type,
            AgvType = agvType,
            Polygon = new Polygon(polygon),
            CalculatedAt = DateTime.UtcNow
        };
    }
}