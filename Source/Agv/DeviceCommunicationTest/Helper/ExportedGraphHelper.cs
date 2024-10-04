using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using System.Numerics;

namespace Gebhardt.Acc.DeviceCommunicationTest.Helper;

public static class ExportedGraphHelper
{
    public static ExportedGraph GenerateDefaultGraph(int stations = 1, int nodes = 2, int chargers=1, string waypointStart= "node/")
    {
        List<ExportedNode> stationNodes = Enumerable.Range(0, stations)
            .Select(s => new ExportedNode($"station{s}", 0, s)).ToList();
        ExportedNode[] wayPoints = Enumerable.Range(0, nodes)
            .Select(n => new ExportedNode($"{waypointStart}{n}", 1, n)).ToArray();
        List<ExportedNode> chargeNodes = Enumerable.Range(0, chargers)
            .Select(s => new ExportedNode($"charger_{s}", 0, s)).ToList();

        ExportedNode[] exportedNodes = [..stationNodes, ..wayPoints, ..chargeNodes];
        List<ExportedSegment> exportedSegments = [];

        for (int i = 0; i < wayPoints.Length -1 ; i++)
        {
            var start = wayPoints[i];
            var end = wayPoints[i + 1];
            ExportedControlPoint[] controlPoints = GenerateLinearControlPoints(start.AsVector2(), end.AsVector2())
                .Select(c => new ExportedControlPoint(c.X, c.Y)).ToArray();

            var segment = new ExportedSegment(){Name=$"{start}-{end}", Points=controlPoints};
            exportedSegments.Add(segment);
            var reverse = new ExportedSegment() { Name=$"{end}-{start}", Points=controlPoints.Reverse().ToArray() };
            exportedSegments.Add(reverse);
        }

        return new ExportedGraph() { Nodes = exportedNodes, Segments = exportedSegments };
    }

    private static Vector2[] GenerateLinearControlPoints(Vector2 start, Vector2 end)
    {
        const float increment = 0.2f;
        return Enumerable.Range(0, 6)
            .Select(i => start + i * increment * (end - start))
            .ToArray();
    }
}