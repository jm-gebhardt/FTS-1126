using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Common.Testing.Helpers;

public static class AgvTypeGenerator
{
    public static AgvTypeInfo GenerateAgvTypeInfo(string agvTypeIdentifier = "KARIS.BF1",
        string manufacturer = "KARIS",
        string seriesName = "BF1",
        double length = 1.0,
        double width = 2.0,
        bool acknowledged = false,
        bool reportedByAgv = false)
    {
        return new AgvTypeInfo(agvTypeIdentifier, manufacturer, seriesName, length, width, acknowledged, reportedByAgv);
    }
}