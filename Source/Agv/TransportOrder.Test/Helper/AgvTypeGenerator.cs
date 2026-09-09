using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

/// <summary>
/// A helper class for generating AGV types for testing purposes.
/// </summary>
public static class AgvTypeGenerator
{
    public static AgvType GenerateAgvType(string agvTypeIdentifier = "KARIS.BF1",
        string manufacturer = "KARIS",
        string seriesName = "BF1",
        double length = 2.0,
        double width = 1.0,
        bool acknowledged = false,
        bool reportedByAgv = false)
    {
        return new AgvType(agvTypeIdentifier, manufacturer, seriesName, length, width, acknowledged, reportedByAgv);
    }
}
