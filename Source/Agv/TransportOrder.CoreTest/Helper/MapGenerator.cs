using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class MapGenerator
{
    public static Map Generate(Guid correlationId,
        string mapId = "map",
        byte[]? mapValue = null,
        int width = 10,
        int height = 20,
        int resolution = 2,
        int xOffset = 0,
        int yOffset = 0,
        DateTime modificationDate = default,
        ICollection<EfNode>? nodes = null)
    {
        return new Map(correlationId, mapId, mapValue, width, height, resolution, xOffset, yOffset, modificationDate != default ? modificationDate : DateTime.UtcNow)
        {
            Nodes = nodes ?? new List<EfNode>()
        };
    }
}