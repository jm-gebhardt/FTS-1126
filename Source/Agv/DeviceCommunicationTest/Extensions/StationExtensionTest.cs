using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Extensions;

[TestClass]
public class StationExtensionTest
{
    [DataRow(Function.Maintenance, AgvGraph.NodeType.Maintenance)]
    [DataRow(Function.Both, AgvGraph.NodeType.PickDrop)]
    [DataRow(Function.Pick, AgvGraph.NodeType.Pick)]
    [DataRow(Function.Drop, AgvGraph.NodeType.Drop)]
    [DataRow(Function.Charge, AgvGraph.NodeType.Charge)]
    [TestMethod]
    public void ToGraphNode_ValidData_ConvertsStationTypeCorrectly(Function function, AgvGraph.NodeType nodeType)
    {
        const float yaxis = 1;
        var exportedStation = new ExportedStation(function, "id", new ExportedPosition(1, 1, yaxis), "");

        var node = exportedStation.ToGraphNode();
        node.Type.ShouldHaveSingleItem().ShouldBe(nodeType);
    }
}