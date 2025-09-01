using Gebhardt.Acc.Statistics.Domain.Models;
using JetBrains.Annotations;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Models;

[TestClass]
[TestSubject(typeof(Map))]
public class MapTest
{
    [TestMethod]
    [DataRow(20, 30, 1, true)]
    [DataRow(15, 70, 1, true)]
    [DataRow(45, 45, 1, true)]
    [DataRow(14, 45, 1, false)]
    [DataRow(15, 24, 1, false)]
    [DataRow(49, 71, 1, false)]
    public void IsPositionWithinMap_ReturnsBooleanValue(int xIndex, int yIndex, float z, bool expected)
    {
        var map = new Map(Guid.NewGuid(), Guid.NewGuid(), 15,25,48,70, 1);
        map.IsPositionWithinMap(xIndex, yIndex, z).ShouldBe(expected);
    }

    [TestMethod]
    public void IsPositionWithinMap_ZPositionOutsideZIdentityPrecision_ReturnsFalse()
    {
        var map = new Map(Guid.NewGuid(), Guid.NewGuid(), 15, 25, 48, 70, 1);
        map.IsPositionWithinMap(map.XMin, map.YMin, map.Z + 1.01f*Domain.Constants.Grid.ZIdentityPrecision).ShouldBeFalse();
    }

    [TestMethod]
    public void IsPositionWithinMap_ZPositionWithinZIdentityPrecision_ReturnsTrue()
    {
        var map = new Map(Guid.NewGuid(), Guid.NewGuid(), 15, 25, 48, 70, 1);
        map.IsPositionWithinMap(map.XMin, map.YMin, map.Z + Domain.Constants.Grid.ZIdentityPrecision).ShouldBeTrue();
    }
}