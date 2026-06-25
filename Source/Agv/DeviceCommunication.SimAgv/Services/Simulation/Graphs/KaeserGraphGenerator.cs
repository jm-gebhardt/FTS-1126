using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using System.Numerics;
using Gebhardt.Acc.Common.Extensions;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// </summary>
public class KaeserGraphGenerator(IMapGenerator mapGenerator) : GraphGeneratorBase(new[] { Pd1, Pd2, Pd3, Pd4, Pd5, Pd6, Pd7, Pd8 }, new[] { Pd1, Pd2, Pd3, Pd4, Pd5, Pd6, Pd7, Pd8 }, mapGenerator)
{
    // Node names

    private const string Pd1 = "1";
    private const string Pd2 = "2";
    private const string Pd3 = "3";
    private const string Pd4 = "4";
    private const string Pd5 = "5";
    private const string Pd6 = "6";
    private const string Pd7 = "7";
    private const string Pd8 = "8";

    private const string Charge0 = "Charger-0";
    private const string Charge1 = "Charger-1";
    private const string Charge2 = "Charger-2";
    private const string Charge3 = "Charger-3";

    private const string Wp00 = "WP-000";
    private const string Wp01 = "WP-001";
    private const string Wp02 = "WP-002";
    private const string Wp03 = "WP-003";
    private const string Wp04 = "WP-004";
    private const string Wp05 = "WP-005";
    private const string Wp06 = "WP-006";
    private const string Wp07 = "WP-007";
    private const string Wp08 = "WP-008";
    private const string Wp09 = "WP-009";
    private const string Wp10 = "WP-010";
    private const string Wp11 = "WP-011";
    private const string Wp12 = "WP-012";
    private const string Wp13 = "WP-013";
    private const string Wp14 = "WP-014";
    private const string Wp15 = "WP-015";
    private const string Wp16 = "WP-016";
    private const string Wp17 = "WP-017";
    private const string Wp18 = "WP-018";
    private const string Wp19 = "WP-019";
    private const string Wp20 = "WP-020";
    private const string Wp21 = "WP-021";
    private const string Wp22 = "WP-022";
    private const string Wp23 = "WP-023";

    private const string Park0 = "ParkQueue-0";
    private const string Park1 = "ParkQueue-1";
    private const string Park2 = "ParkQueue-2";

    private const string ManualCharge = "ManualCharge";

    private const float North = (float)Math.PI / 2f;
    private const float South = -(float)Math.PI / 2f;

    private const float StationSpacing = 2f;

    private const float RowSpacing = 2f;

    private const float X5 = 5f;
    private const float X6 = X5 + StationSpacing;
    private const float X7 = X6 + StationSpacing;
    private const float X8 = X7 + StationSpacing;

    private const float X3 = X7 + StationSpacing;
    private const float X2 = X3 + StationSpacing;
    private const float X1 = X2 + StationSpacing;

    private const float Xwp17 = 31f;

    private const float Cx0 = 21f;
    private const float Cx1 = Cx0 + StationSpacing;
    private const float Cx2 = Cx1 + StationSpacing;
    private const float Cx3 = Cx2 + StationSpacing;

    private const float Y4 = -21f;
    private const float Ywp17 = -13f;
    private const float Ywp18 = -19f;

    private const float Row0 = -5f;
    private const float Row1 = Row0 - RowSpacing;
    private const float Row2 = Row1 - RowSpacing;
    private const float Row3 = Row2 - RowSpacing;
    private const float EmptyRow = Row3 - RowSpacing;
    private const float Row4 = EmptyRow - RowSpacing;
    private const float Row5 = Row4 - RowSpacing;
    private const float Row6 = Row5 - RowSpacing;

    private const float Col0 = X8;
    private const float Col1 = X2;
    private const float Col2 = Cx0;
    private const float Col3 = Cx1;
    private const float Col4 = Cx2;
    private const float Col5 = Cx3;

    private readonly DateTime _timeStamp = DateTime.UtcNow;

    /// <inheritdoc />
    protected override Guid GraphId { get; } = "KaeserGraph".ToGuid();

    /// <inheritdoc />
    public override AgvGraph Generate(Guid correlationId)
    {
        var nodes = new GraphNode[]
        {
            new(Charge0.ToGuid(), Charge0) { Type = [AgvGraph.NodeType.Charge], Theta = North, X = Col2, Y = Row0, TimeStamp = _timeStamp },
            new(Charge1.ToGuid(), Charge1) { Type = [AgvGraph.NodeType.Charge], Theta = North, X = Col3, Y = Row0, TimeStamp = _timeStamp },
            new(Charge2.ToGuid(), Charge2) { Type = [AgvGraph.NodeType.Charge], Theta = North, X = Col4, Y = Row0, TimeStamp = _timeStamp },
            new(Charge3.ToGuid(), Charge3) { Type = [AgvGraph.NodeType.Charge], Theta = North, X = Col5, Y = Row0, TimeStamp = _timeStamp },
            new(ManualCharge.ToGuid(), ManualCharge) { Type = [AgvGraph.NodeType.Charge], Theta = North, X = X7, Y = Row1, TimeStamp = _timeStamp },

            new(Pd1.ToGuid(), Pd1) { Type = [AgvGraph.NodeType.PickDrop], Theta = North, X = X1, Y = Row1, TimeStamp = _timeStamp },
            new(Pd2.ToGuid(), Pd2) { Type = [AgvGraph.NodeType.PickDrop], Theta = North, X = X2, Y = Row1, TimeStamp = _timeStamp },
            new(Pd3.ToGuid(), Pd3) { Type = [AgvGraph.NodeType.PickDrop], Theta = North, X = X3, Y = Row1, TimeStamp = _timeStamp },
            new(Pd4.ToGuid(), Pd4) { Type = [AgvGraph.NodeType.PickDrop], Theta = South, X = Col5, Y = Y4, TimeStamp = _timeStamp },
            new(Pd5.ToGuid(), Pd5) { Type = [AgvGraph.NodeType.PickDrop], Theta = South, X = X5, Y = Row6, TimeStamp = _timeStamp },
            new(Pd6.ToGuid(), Pd6) { Type = [AgvGraph.NodeType.PickDrop], Theta = South, X = X6, Y = Row6, TimeStamp = _timeStamp },
            new(Pd7.ToGuid(), Pd7) { Type = [AgvGraph.NodeType.PickDrop], Theta = South, X = X7, Y = Row6, TimeStamp = _timeStamp },
            new(Pd8.ToGuid(), Pd8) { Type = [AgvGraph.NodeType.PickDrop], Theta = South, X = Col0, Y = Row6, TimeStamp = _timeStamp },

            new(Park2.ToGuid(), Park2) { Type = [AgvGraph.NodeType.Park], Theta = South, X = Col5, Y = Ywp17, TimeStamp = _timeStamp },
            new(Park1.ToGuid(), Park1) { Type = [AgvGraph.NodeType.Park], Theta = South, X = Col5, Y = Row4, TimeStamp = _timeStamp },
            new(Park0.ToGuid(), Park0) { Type = [AgvGraph.NodeType.Park], Theta = South, X = Col5, Y = Row5, TimeStamp = _timeStamp },

            new(Wp00.ToGuid(), Wp00) { Type = [AgvGraph.NodeType.WayPoint], X = X5, Y = Row5, TimeStamp = _timeStamp },
            new(Wp01.ToGuid(), Wp01) { Type = [AgvGraph.NodeType.WayPoint], X = X6, Y = Row5, TimeStamp = _timeStamp },
            new(Wp02.ToGuid(), Wp02) { Type = [AgvGraph.NodeType.WayPoint], X = X7, Y = Row5, TimeStamp = _timeStamp },
            new(Wp03.ToGuid(), Wp03) { Type = [AgvGraph.NodeType.WayPoint], X = Col0, Y = Row5, TimeStamp = _timeStamp },
            new(Wp04.ToGuid(), Wp04) { Type = [AgvGraph.NodeType.WayPoint], X = Col0, Y = Row3, TimeStamp = _timeStamp },
            new(Wp05.ToGuid(), Wp05) { Type = [AgvGraph.NodeType.WayPoint], X = Col1, Y = Row3, TimeStamp = _timeStamp },
            new(Wp06.ToGuid(), Wp06) { Type = [AgvGraph.NodeType.WayPoint], X = Col1, Y = Row2, TimeStamp = _timeStamp },
            new(Wp07.ToGuid(), Wp07) { Type = [AgvGraph.NodeType.WayPoint], X = X1, Y = Row2, TimeStamp = _timeStamp },
            new(Wp08.ToGuid(), Wp08) { Type = [AgvGraph.NodeType.WayPoint], X = Col2, Y = Row1, TimeStamp = _timeStamp },
            new(Wp09.ToGuid(), Wp09) { Type = [AgvGraph.NodeType.WayPoint], X = Col3, Y = Row1, TimeStamp = _timeStamp },
            new(Wp10.ToGuid(), Wp10) { Type = [AgvGraph.NodeType.WayPoint], X = Col4, Y = Row1, TimeStamp = _timeStamp },
            new(Wp11.ToGuid(), Wp11) { Type = [AgvGraph.NodeType.WayPoint], X = Col5, Y = Row1, TimeStamp = _timeStamp },
            new(Wp12.ToGuid(), Wp12) { Type = [AgvGraph.NodeType.WayPoint], X = Col2, Y = Row2, TimeStamp = _timeStamp },
            new(Wp13.ToGuid(), Wp13) { Type = [AgvGraph.NodeType.WayPoint], X = Col3, Y = Row2, TimeStamp = _timeStamp },
            new(Wp14.ToGuid(), Wp14) { Type = [AgvGraph.NodeType.WayPoint], X = Col4, Y = Row2, TimeStamp = _timeStamp },
            new(Wp15.ToGuid(), Wp15) { Type = [AgvGraph.NodeType.WayPoint], X = Col5, Y = Row2, TimeStamp = _timeStamp },
            new(Wp16.ToGuid(), Wp16) { Type = [AgvGraph.NodeType.WayPoint], X = Col2, Y = Row3, TimeStamp = _timeStamp },
            new(Wp17.ToGuid(), Wp17) { Type = [AgvGraph.NodeType.WayPoint], X = Xwp17, Y = Ywp17, TimeStamp = _timeStamp },
            new(Wp18.ToGuid(), Wp18) { Type = [AgvGraph.NodeType.WayPoint], X = Col5, Y = Ywp18, TimeStamp = _timeStamp },
            new(Wp19.ToGuid(), Wp19) { Type = [AgvGraph.NodeType.WayPoint], X = X1 + 2f * (Col2 - X1) / 5f, Y = Row2, TimeStamp = _timeStamp },
            new(Wp20.ToGuid(), Wp20) { Type = [AgvGraph.NodeType.WayPoint], X = X1 + 3f * (Col2 - X1) / 5f, Y = Row2, TimeStamp = _timeStamp },
            new(Wp21.ToGuid(), Wp21) { Type = [AgvGraph.NodeType.WayPoint], X = X1 + 4f * (Col2 - X1) / 5f, Y = Row2, TimeStamp = _timeStamp },
            new(Wp22.ToGuid(), Wp22) { Type = [AgvGraph.NodeType.WayPoint], X = Col0, Y = Row2, TimeStamp = _timeStamp },
            new(Wp23.ToGuid(), Wp23) { Type = [AgvGraph.NodeType.WayPoint], X = Col0, Y = Row4, TimeStamp = _timeStamp }
        };
        GraphEdge[] edges =
        {
            CreateDirect(Wp00, Wp01, nodes),
            CreateDirect(Wp01, Wp00, nodes),
            CreateDirect(Wp01, Wp02, nodes),
            CreateDirect(Wp02, Wp01, nodes),
            CreateDirect(Wp02, Wp03, nodes),
            CreateDirect(Wp03, Wp02, nodes),
            CreateDirect(Wp03, Wp23, nodes),
            CreateDirect(Wp23, Wp03, nodes),
            CreateDirect(Wp04, Wp23, nodes),
            CreateDirect(Wp23, Wp04, nodes),
            CreateDirect(Wp04, Wp05, nodes),
            CreateDirect(Wp05, Wp04, nodes),
            CreateDirect(Wp05, Wp06, nodes),
            CreateDirect(Wp06, Wp05, nodes),
            CreateDirect(Wp05, Wp16, nodes),
            CreateDirect(Wp07, Wp06, nodes),
            CreateDirect(Wp12, Wp21, nodes),
            CreateDirect(Wp21, Wp20, nodes),
            CreateDirect(Wp20, Wp19, nodes),
            CreateDirect(Wp19, Wp07, nodes),
            CreateDirect(Wp12, Wp16, nodes),
            CreateDirect(Wp16, Wp12, nodes),
            CreateDirect(Wp08, Wp12, nodes),
            CreateDirect(Wp12, Wp08, nodes),
            CreateDirect(Wp09, Wp13, nodes),
            CreateDirect(Wp13, Wp09, nodes),
            CreateDirect(Wp10, Wp14, nodes),
            CreateDirect(Wp14, Wp10, nodes),
            CreateDirect(Wp11, Wp15, nodes),
            CreateDirect(Wp15, Wp11, nodes),
            CreateDirect(Wp09, Wp08, nodes),
            CreateDirect(Wp10, Wp09, nodes),
            CreateDirect(Wp11, Wp10, nodes),
            CreateDirect(Wp13, Wp12, nodes),
            CreateDirect(Wp14, Wp13, nodes),
            CreateDirect(Wp15, Wp14, nodes),
            CreateDirect(Wp06, Wp22, nodes),
            CreateDirect(Wp04, Wp22, nodes),
            CreateDirect(Wp22, Wp04, nodes),

            CreateDirect(Park2, Park1, nodes),
            CreateDirect(Park2, Wp17, nodes),
            CreateDirect(Park1, Park0, nodes),
            CreateDirect(Park0, Wp18, nodes),

            CreateDirect(Wp18, Pd4, nodes),
            CreateDirect(Pd4, Wp18, nodes),

            CreateDirect(Charge0, Wp08, nodes),
            CreateDirect(Wp08, Charge0, nodes),
            CreateDirect(Charge1, Wp09, nodes),
            CreateDirect(Wp09, Charge1, nodes),
            CreateDirect(Charge2, Wp10, nodes),
            CreateDirect(Wp10, Charge2, nodes),
            CreateDirect(Charge3, Wp11, nodes),
            CreateDirect(Wp11, Charge3, nodes),

            CreateDirect(Pd5, Wp00, nodes),
            CreateDirect(Wp00, Pd5, nodes),
            CreateDirect(Pd6, Wp01, nodes),
            CreateDirect(Wp01, Pd6, nodes),
            CreateDirect(Pd7, Wp02, nodes),
            CreateDirect(Wp02, Pd7, nodes),
            CreateDirect(Pd8, Wp03, nodes),
            CreateDirect(Wp03, Pd8, nodes),

            CreateDirect(Pd1, Wp07, nodes),
            CreateDirect(Wp07, Pd1, nodes),
            CreateDirect(Pd2, Wp06, nodes),
            CreateDirect(Wp06, Pd2, nodes),
            CreateDirect(Pd3, Wp22, nodes),
            CreateDirect(Wp22, Pd3, nodes),

            CreatePolypath(Wp16, Park2, [new EdgeCoefficient(Col5, Row3)], nodes),
            CreatePolypath(Park1, Wp17, [new EdgeCoefficient(Xwp17, Row4)], nodes),
            CreatePolypath(Wp17, Wp11, [new EdgeCoefficient(Xwp17, Row1)], nodes),
            CreatePolypath(Wp18, Wp15, [new EdgeCoefficient(Xwp17, Ywp18), new EdgeCoefficient(Xwp17, Row2)], nodes),
            CreatePolypath(ManualCharge, Wp22, [new EdgeCoefficient(X7, Row2)], nodes),
            CreatePolypath(Wp22, ManualCharge, [new EdgeCoefficient(X7, Row2)], nodes)
        };
        return new AgvGraph(correlationId, nodes, edges);
    }

    private GraphEdge CreateDirect(string fromNodeId, string toNodeId, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Name == fromNodeId);
        var targetNode = nodes.Single(n => n.Name == toNodeId);
        float length = (float)Mathematics.EuclideanDistance(sourceNode, targetNode);
        return GraphEdge.CreateDirect(fromNodeId.ToGuid(), toNodeId.ToGuid(), length, GraphHelper.ToGuid(EdgeType.Direct, fromNodeId, length, toNodeId), _timeStamp);
    }

    private GraphEdge CreatePolypath(string fromNodeId, string toNodeId, List<EdgeCoefficient> pathPoints, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Name == fromNodeId).AsVector3();
        var targetNode = nodes.Single(n => n.Name == toNodeId).AsVector3();
        List<Vector3> points = [sourceNode];
        points.AddRange(pathPoints.Select(c => new Vector3((float)c.X!, (float)c.Y!, 0f)));
        points.Add(targetNode);
        double length = new Polypath(points).GetLength();
        return GraphEdge.CreatePolypath(fromNodeId.ToGuid(), toNodeId.ToGuid(), (float)length, pathPoints, GraphHelper.ToGuid(EdgeType.Polypath, fromNodeId, (float)length, toNodeId), _timeStamp);
    }
}