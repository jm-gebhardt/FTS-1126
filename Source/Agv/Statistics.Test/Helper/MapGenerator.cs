using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Test.Helper;

public static class MapGenerator
{
    public static Map GenerateMap(int xMin = 65, int yMin = 27, int xMax = 70, int yMax = 95, Guid? mapId = null, Guid? gridId = null) =>
        new(mapId ?? Guid.NewGuid(), gridId ?? Guid.NewGuid(), xMin, yMin, xMax, yMax);
}