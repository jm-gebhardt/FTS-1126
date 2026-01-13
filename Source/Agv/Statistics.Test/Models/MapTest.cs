using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Helper;
using JetBrains.Annotations;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Models;

[TestClass]
[TestSubject(typeof(Map))]
public class MapTest
{
    [TestMethod]
    [DataRow(20, 30, true)]
    [DataRow(15, 70, true)]
    [DataRow(45, 45, true)]
    [DataRow(14, 45, false)]
    [DataRow(15, 24, false)]
    [DataRow(49, 71, false)]
    public void IsWithinMap_ReturnsBooleanValue(int xIndex, int yIndex, bool expected)
    {
        var map = MapGenerator.GenerateMap( 15, 25, 48, 70);
        map.IsWithinMap(xIndex, yIndex).ShouldBe(expected);
    }
}