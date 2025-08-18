using Gebhardt.Acc.TransportOrder.Domain.Models.Map;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class MapBackgroundGenerator
{
    // 1x1
    public static readonly byte[] Image1 =
    [
        137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0, 31, 21, 196, 137, 0, 0, 0, 4, 115, 66, 73, 84, 8, 8, 8, 8, 124, 8, 100, 136, 0, 0, 0, 11, 73, 68, 65, 84, 8, 153, 99, 96, 0, 2, 0, 0, 5, 0, 1, 98, 85, 50, 136, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130
    ];

    public static MapBackground Generate(
        byte[]? image = null,
        Guid correlationId = default,
        int width = 10,
        int height = 20,
        double resolution = 2,
        double xOffset = 0,
        double yOffset = 0,
        bool isCustom = false
    )
    {
        if (correlationId == Guid.Empty)
        {
            correlationId = Guid.NewGuid();
        }
        image ??= Image1;

        return new MapBackground(correlationId, resolution, xOffset, yOffset, width, height, image, isCustom);
    }
}