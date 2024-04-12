using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.Statistics.Test.Models;

[TestClass]
[TestSubject(typeof(Map))]
public class MapTest
{
    [DataTestMethod]
    [DataRow(20, 30, true)]
    [DataRow(15, 70, true)]
    [DataRow(45, 45, true)]
    [DataRow(14, 45, false)]
    [DataRow(15, 24, false)]
    [DataRow(49, 71, false)]
    public void IsPositionWithinMap_ReturnsBooleanValue(int xIndex, int yIndex, bool expected)
    {
        var map = new Map(Guid.NewGuid(), Guid.NewGuid(), 15,25,48,70);
        map.IsPositionWithinMap(xIndex, yIndex).ShouldBe(expected);
    }
}