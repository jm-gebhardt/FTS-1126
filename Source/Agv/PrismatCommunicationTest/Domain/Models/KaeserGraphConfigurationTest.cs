using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Models;

[TestClass]
public class KaeserGraphConfigurationTest
{
    [TestMethod]
    public void UpdateLiftHeights_ValidData_UpdatesTargetLiftHeight()
    {
        const string updatedKey = "2110";
        const double updatedValue = 1.444d;
        const string newKey = "new";
        const double newValue = 1.555d;
        var kaeserGraph = new KaeserGraphConfiguration();
        kaeserGraph.TargetLiftHeight["2110"].ShouldBe(2.211d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2130"].ShouldBe(1.213d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2210"].ShouldBe(2.221d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2230"].ShouldBe(1.223d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2310"].ShouldBe(2.231d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2330"].ShouldBe(1.233d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2410"].ShouldBe(2.241d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2430"].ShouldBe(1.243d, tolerance: double.Epsilon);

        kaeserGraph.UpdateLiftHeights(new Dictionary<string, double> { { updatedKey, updatedValue }, { newKey, newValue } });
        kaeserGraph.TargetLiftHeight[newKey].ShouldBe(newValue, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight[updatedKey].ShouldBe(updatedValue, tolerance: double.Epsilon);
    }

    [DataRow("2110", true)]
    [DataRow("2210", true)]
    [DataRow("2310", true)]
    [DataRow("2410", true)]
    [DataRow("2130", false)]
    [DataRow("2230", false)]
    [DataRow("2330", false)]
    [DataRow("2430", false)]
    [DataRow("1050", false)]
    [DataRow("1060", false)]
    [DataRow("1020", false)]
    [DataRow("1040", false)]
    [DataRow("9060", false)]
    [DataRow("9070", false)]
    [DataTestMethod]
    public void IsDropAtAkl_ReturnsExpectedValue(string target, bool expected)
    {
        var kaeserGraph = new KaeserGraphConfiguration();
        kaeserGraph.IsDropAtAkl(target).ShouldBe(expected);
    }

    [DataRow("2110", false)]
    [DataRow("2210", false)]
    [DataRow("2310", false)]
    [DataRow("2410", false)]
    [DataRow("2130", true)]
    [DataRow("2230", true)]
    [DataRow("2330", true)]
    [DataRow("2430", true)]
    [DataRow("1050", false)]
    [DataRow("1060", false)]
    [DataRow("1020", false)]
    [DataRow("1040", false)]
    [DataRow("9060", false)]
    [DataRow("9070", false)]
    [DataTestMethod]
    public void IsPickAtAkl_ReturnsExpectedValue(string target, bool expected)
    {
        var kaeserGraph = new KaeserGraphConfiguration();
        kaeserGraph.IsPickAtAkl(target).ShouldBe(expected);
    }

    [TestMethod]
    public void IsOccupancyLimitedNode_ConveyorStation_ReturnsTrue()
    {
        var kaeserGraph = OrderGenerator.KaeserConfiguration;

        kaeserGraph.IsOccupancyLimitedNode(kaeserGraph.ConveyorStationMapping.Keys.First()).ShouldBeTrue();
    }

    [TestMethod]
    public void IsOccupancyLimitedNode_KaeserPickDropStation_ReturnsTrue()
    {
        var kaeserGraph = OrderGenerator.KaeserConfiguration;

        kaeserGraph.IsOccupancyLimitedNode(kaeserGraph.StationMapping.Values.First()).ShouldBeTrue();
    }

    [TestMethod]
    public void IsOccupancyLimitedNode_ChargerOrWaypoint_ReturnsFalse()
    {
        var kaeserGraph = OrderGenerator.KaeserConfiguration;

        kaeserGraph.IsOccupancyLimitedNode("Waypoint").ShouldBeFalse();
    }
}