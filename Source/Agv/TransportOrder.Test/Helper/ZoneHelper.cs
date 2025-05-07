using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class ZoneHelper
{
    public static Zone GenerateZoneFromPosition( AgvPositionStatus position, bool isInside = false, Guid? zoneId = null)
    {
        const string zoneName = "a zone";
        var zoneCorrelationId = zoneId ?? zoneName.ToGuid();
        List<ZonePolygonPoint> zonePoints;
        if (isInside)
        {
            zonePoints = new List<ZonePolygonPoint>
            {
                new(position.AgvPosition.X - 1, position.AgvPosition.Y, 0, zoneCorrelationId),
                new(position.AgvPosition.X, position.AgvPosition.Y - 1, 1, zoneCorrelationId),
                new(position.AgvPosition.X + 1, position.AgvPosition.Y, 2, zoneCorrelationId),
                new(position.AgvPosition.X, position.AgvPosition.Y + 1, 3, zoneCorrelationId)
            };
            return new Zone(zonePoints, zoneName, zoneCorrelationId, 1);
        }

        zonePoints = new List<ZonePolygonPoint>
        {
            new(position.AgvPosition.X - 1, position.AgvPosition.Y, 0, zoneCorrelationId),
            new(position.AgvPosition.X - 2, position.AgvPosition.Y - 1, 1, zoneCorrelationId),
            new(position.AgvPosition.X - 3, position.AgvPosition.Y, 2, zoneCorrelationId),
            new(position.AgvPosition.X - 2, position.AgvPosition.Y + 1, 3, zoneCorrelationId)
        };
        return new Zone(zonePoints, zoneName, zoneCorrelationId, 1);
    }
}